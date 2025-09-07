using SharpLoader;
using SharpLoader.Core.Modding;
using SharpLoader.Modding;
using SharpLoader.Utilities;

namespace SharpLoaderExample;

public class ModuleMain : ModuleBase
{
    public static ModuleMain? Instance { get; private set; }

    public ModuleMain()
    {
        Instance = this;
    }

    protected override void OnInitialize(ModuleManager manager, LoggerService? logger)
    {
        Logger?.Info("Module loaded.");
    }
    

    public override byte[]? ModifyClass(string className, byte[] classData)
    {
        if (Manager == null) return null;
        var classMappedName = Manager.Mapping.Classes.TryGetValue(className, out var mappedClass);
        var innerClassMappedName = Manager.Mapping.InnerClasses.TryGetValue(className, out var mappedInnerClass);
        if (classMappedName) Logger?.Trace($"(Mappings) {className} => {mappedClass?.MappedName}");
        if (innerClassMappedName) Logger?.Trace($"(Mappings) {className} => {mappedInnerClass?.MappedName}");
        return null;
    }
}