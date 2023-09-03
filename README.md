# Unity Android Hang Repro project

Unity project to replicate hanging issue under Android. 

## Brief description

Doing work on multiple threads (how outrageous) while also asynchronously loading a scene there's a good chance under Android that the application enters into a dead state.

## Reproduction steps

1. `StartupManager` creates a thread and executes a DNS query and then connect through Tcp to a server (google.com in this case), and finally does a TLS authentication step using `SslStream`.
2. While doing these work, queues up logging entries to a `ThreadedLogger` that formats the entries and writes them out using Debug.Log.
3. Just after 1., start the loading of the second scene using `SceneManager.LoadSceneAsync`.
4. When (and if) the new scene is loaded, execute 1. the second time.
5. If both execution of 1. succeeds, restart the application to do a new repro-turn.

Adding more work on additional threads are increasing reproducibility.

## Stack Traces

This stack trace is always the same and it's from Unity's main thread:

>  (syscall+32)
>  (il2cpp_baselib::Baselib_SystemFutex_Wait(int*, int, unsigned int)+76)
>  (Baselib_Lock_Acquire(Baselib_Lock*)+304)
>  (Baselib_ReentrantLock_Acquire(Baselib_ReentrantLock*)+100)
>  (baselib::il2cpp_baselib::ReentrantLock::Acquire()+20)
>  (il2cpp::os::FastAutoLock::FastAutoLock(baselib::il2cpp_baselib::ReentrantLock*)+52)
>  (il2cpp::vm::GlobalMetadata::GetTypeInfoFromTypeDefinitionIndex(int)+224)
>  (il2cpp::vm::GlobalMetadata::GetTypeInfoFromHandle(___Il2CppMetadataTypeHandle const*)+32)
>  (il2cpp::vm::GlobalMetadata::GetTypeInfoFromType(Il2CppType const*)+24)
>  (il2cpp::vm::MetadataCache::GetTypeInfoFromType(Il2CppType const*)+52)
>  (il2cpp::vm::Type::IsStruct(Il2CppType const*)+96)
>  (il2cpp::vm::LivenessState::FieldCanContainReferences(FieldInfo*)+24)
>  (il2cpp::vm::Liveness::FromStatics(void*)+280)
>  (il2cpp_unity_liveness_calculation_from_statics+20)
>  (GarbageCollectSharedAssets(bool, bool)+980)
>  (UnloadUnusedAssetsOperation::IntegrateMainThread()+16)
>  (PreloadManager::UpdatePreloadingSingleStep(PreloadManager::UpdatePreloadingFlags, int)+276)
>  (PreloadManager::UpdatePreloading()+284)
>  (???)
>  (ExecutePlayerLoop(NativePlayerLoopSystem*)+92)
>  (ExecutePlayerLoop(NativePlayerLoopSystem*)+156)
>  (PlayerLoop()+312)
>  (UnityPlayerLoop()+824)
>  (nativeRender(_JNIEnv*, _jobject*)+72)

Common stack trace (along with the above one) is that goes through the `MetadataCache`:
>  (__rt_sigsuspend+8)
>  (sigsuspend+52)
>  (GC_suspend_handler_inner+204)
>  (GC_suspend_handler+112)
>  (signal_handler+108)
>  [vdso] (__kernel_rt_sigreturn)
>  (operator new(unsigned long)+44)
>  (std::__ndk1::__libcpp_allocate(unsigned long, unsigned long)+24)
>  (std::__ndk1::allocator<Il2CppClass*>::allocate(unsigned long, void const*)+84)
>  (std::__ndk1::allocator_traits<std::__ndk1::allocator<Il2CppClass*>>::allocate(std::__ndk1::allocator<Il2CppClass*>&, unsigned long)+36)
>  (std::__ndk1::__split_buffer<Il2CppClass*, std::__ndk1::allocator<Il2CppClass*>&>::__split_buffer(unsigned long, unsigned long, std::__ndk1::allocator<Il2CppClass*>&)+88)
>  (void std::__ndk1::vector<Il2CppClass*, std::__ndk1::allocator<Il2CppClass*>>::__push_back_slow_path<Il2CppClass* const&>(Il2CppClass* const&&&)+108)
>  (std::__ndk1::vector<Il2CppClass*, std::__ndk1::allocator<Il2CppClass*>>::push_back(Il2CppClass* const&)+88)
>  (il2cpp::metadata::CollectImplicitArrayInterfacesFromElementClass(Il2CppClass*, std::__ndk1::vector<Il2CppClass*, std::__ndk1::allocator<Il2CppClass*>>&)+60)
>  (il2cpp::metadata::ArrayMetadata::GetBoundedArrayClass(Il2CppClass*, unsigned int, bool)+348)
>  (il2cpp::vm::Class::GetBoundedArrayClass(Il2CppClass*, unsigned int, bool)+48)
>  (il2cpp::vm::Class::GetArrayClass(Il2CppClass*, unsigned int)+40)
>  (il2cpp::vm::Class::FromIl2CppType(Il2CppType const*, bool)+328)
>  (il2cpp::metadata::GenericMetadata::InflateRGCTXLocked(Il2CppImage const*, unsigned int, Il2CppGenericContext const*, il2cpp::os::FastAutoLock const&)+300)
>  (il2cpp::vm::Class::InitLocked(Il2CppClass*, il2cpp::os::FastAutoLock const&)+1508)
>  (il2cpp::vm::Class::Init(Il2CppClass*)+204)
>  (il2cpp::vm::ClassInlines::InitFromCodegenSlow(Il2CppClass*)+76)
>  (il2cpp::vm::ClassInlines::InitFromCodegenSlow(Il2CppClass*, bool)+100)
>  (il2cpp::vm::GlobalMetadata::GetTypeInfoFromTypeIndex(int, bool)+340)
>  (il2cpp::vm::GlobalMetadata::InitializeRuntimeMetadata(unsigned long*, bool)+192)
>  (il2cpp::vm::MetadataCache::InitializeRuntimeMetadata(unsigned long*)+32)
>  (il2cpp_codegen_initialize_runtime_metadata(unsigned long*)+20)
>  (ServicePointManager__cctor_mB2159CD3E1D15E7F0C3D395EC4B004696A8ACEFB+64)
>  (_Z66RuntimeInvoker_FalseVoid_t4861ACF8F4594C3437BB48B6E56783494B843915PFvvEPK10MethodInfoPvPS4_S4_+40)
>  (il2cpp::vm::Runtime::InvokeWithThrow(MethodInfo const*, void*, void**)+96)
>  (il2cpp::vm::Runtime::Invoke(MethodInfo const*, void*, void**, Il2CppException**)+228)
>  (il2cpp::vm::Runtime::ClassInit(Il2CppClass*)+504)
>  (il2cpp_codegen_runtime_class_init(Il2CppClass*)+20)
>  (il2cpp_codegen_runtime_class_init_inline(Il2CppClass*)+32)

## Known workarounds

None.
A few combination of the player settings can delay or reduce the chance of the issue, could find any combination that would ultimately solve it.
While other threads are suspended in the `GC_suspend_handler`, turning on/off incremental GC has no meaningfull effect on it.
Could reprouduce it on all LTS versions of Unity and with different Android devices. However it seems it's reproducable only on Android.

## Forewords

The bug reported to Unity on 2023-08-16 (assigned case id is IN-51557), but at the writing of this readme (2023-09-03) nothing happened with the case.

Thanks goes to *Pavel Novikov* at highcore.io who sent the initial repro project to me. His project used Best HTTP/2, but i could remove this dependency and replicate the same bug without any 3rd party code minimising confusion and clutter.