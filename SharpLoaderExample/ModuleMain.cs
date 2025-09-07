using SharpLoader;
using SharpLoader.Core.Modding;
using SharpLoader.Modding;
using SharpLoader.Utilities;

namespace SharpLoaderExample;

public class ModuleMain : IModule
{
    public IntPtr JvmHandle { get; set; }
    public IntPtr EnvHandle { get; set; }
    
    public static ModuleMain? Instance { get; private set; }
    public static ModuleManager? Manager { get; private set; }
    public static LoggerService? Logger { get; private set; }

    public ModuleMain()
    {
        Instance = this;
    }

    public bool Setup(IntPtr jvm, IntPtr env)
    {
        JvmHandle = jvm;
        EnvHandle = env;
        return true;
    }

    public void Initialize(ModuleManager manager, LoggerService? logger)
    {
        Manager = manager;
        Logger = logger;

        Logger?.Info("Module Initialized.");
    }

    public byte[]? ModifyClass(string className, byte[] classData)
    {
        if (Manager == null) return null;
        var classMappedName = Manager.Mapping.Classes.TryGetValue(className, out var mappedClass);
        var innerClassMappedName = Manager.Mapping.InnerClasses.TryGetValue(className, out var mappedInnerClass);
        if (classMappedName) Logger?.Trace($"(Mappings) {className} => {mappedClass?.MappedName}");
        if (innerClassMappedName) Logger?.Trace($"(Mappings) {className} => {mappedInnerClass?.MappedName}");
        return null;
    }
}