#include "pch.h"

class CCOMLoaderClassFactory :
    public CComObjectRootEx<CComMultiThreadModel>,
    public IClassFactory
{
private: // ivars
    msclr::auto_gcroot<System::Type ^> type = nullptr;

    HRESULT Initialize(System::Type^ type) {
        using namespace System;
        using namespace System::Runtime::InteropServices;

        // Types used with CCOMLoaderClassFactory require [ComVisible(true)] and [Guid] to be applied.
        try
        {
            auto attributes = type->GetCustomAttributes(ComVisibleAttribute::typeid, false);
            if (attributes->Length == 0) return E_INVALIDARG;

            auto comVisibleAttribute = safe_cast<ComVisibleAttribute^>(attributes[0]);
            if (comVisibleAttribute != nullptr || !comVisibleAttribute->Value) return E_INVALIDARG;

            attributes = type->GetCustomAttributes(GuidAttribute::typeid, false);
            if (attributes->Length == 0) return E_INVALIDARG;
        }
        catch (Exception ^ ex) {
            return ex->HResult;
        }

        this->type.attach(type);
        return S_OK;
    }

public: // Constructors
    template<typename T>
    static GUID GetGUIDFromType(void) {
        System::Guid managedGuid = T::typeid->GUID;
        return *reinterpret_cast<GUID*>(&managedGuid);
    }

    static HRESULT Create(System::Type^ type, REFIID iid, void** ppObject) {
        CComObject<CCOMLoaderClassFactory>* object;
        HRESULT hr = CComObject<CCOMLoaderClassFactory>::CreateInstance(&object);
        if (FAILED(hr)) return hr;

        object->AddRef();
        hr = object->Initialize(type);

        if (SUCCEEDED(hr)) hr = object->QueryInterface(iid, ppObject);
        object->Release();

        return hr;
    }

public:
    STDMETHOD(CreateInstance)(IUnknown* pUnkOuter, REFIID desiredIID, void** ppObject) {
        if (pUnkOuter != nullptr) return CLASS_E_NOAGGREGATION;
        if (ppObject == nullptr) return E_INVALIDARG;

        // TODO: How do I correctly handle this?
        if (desiredIID == IID_IUnknown) return E_INVALIDARG;

        try {
            System::Object^ obj = System::Activator::CreateInstance(type.get());
            System::Type^ desiredInterfaceType = nullptr;

            for each (auto ifaceType in type.get()->GetInterfaces()) {
                GUID ifaceIID = *reinterpret_cast<GUID*>(&ifaceType->GUID);
                if (ifaceIID == desiredIID) {
                    desiredInterfaceType = ifaceType;
                    break;
                }
            }

            if (desiredInterfaceType == nullptr) return E_NOINTERFACE;

            // This call will throw if the cast doesn't work.
            System::IntPtr ptr = System::Runtime::InteropServices::Marshal::GetComInterfaceForObject(obj, desiredInterfaceType);
            *ppObject = (void*)ptr;
            return S_OK;
        }
        catch (System::Exception ^ ex) {
            return ex->HResult;
        }
    }

    STDMETHOD(LockServer)(BOOL fLock) {
        return E_NOTIMPL;
    }

public:
    DECLARE_NO_REGISTRY()
    DECLARE_PROTECT_FINAL_CONSTRUCT()

    BEGIN_COM_MAP(CCOMLoaderClassFactory)
        COM_INTERFACE_ENTRY(IClassFactory)
    END_COM_MAP()
};

#define TRY_CREATE_CF_FOR_TYPE(T) \
    if (clsid == CCOMLoaderClassFactory::GetGUIDFromType<T>()) hr = CCOMLoaderClassFactory::Create(T::typeid, IID_PPV_ARGS(&cf));

EXTERN_C HRESULT WINAPI DllGetClassObject(REFCLSID clsid, REFIID iid, void** ppObject) {
    HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;
    CComPtr<IClassFactory> cf;
    TRY_CREATE_CF_FOR_TYPE(MSIExtract::ShellExtension::MSIViewerOpenCommand);

    if (FAILED(hr)) return hr;
    hr = cf->QueryInterface(iid, ppObject);
    return hr;
}

EXTERN_C HRESULT WINAPI DllCanUnloadNow(void) {
    return S_FALSE;
}
