using Microsoft.Win32;

namespace Application;

internal static class EnvironmentVariable
{
    public static void Set(string variableName, string variableValue)
    {
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", variableName, variableValue, RegistryValueKind.String);
    }
    public static string Get(string variableName)
    {
        return (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", variableName, null);
    }
}
