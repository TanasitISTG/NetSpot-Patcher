using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace ConsoleApp1;

public class Utils
{
    public void Run()
    {
        var path = AutoFindFile();
        BackupFiles(path);
        var exeModule = ModuleDefMD.Load(path + @"\NetSpot.Backup.exe");
        var dllModule = ModuleDefMD.Load(path + @"\NetSpot.Core.Policies.Backup.dll");
        Console.WriteLine("Patching...");
        PatchLicense(dllModule);
        PatchIntegrityCheck(exeModule);
        PatchAboutForm(exeModule);
        Console.WriteLine("Saving...");
        SaveModule(exeModule, path + @"\NetSpot.exe");
        SaveModule(dllModule, path + @"\NetSpot.Core.Policies.dll");
        Console.WriteLine("Suceessfully patched!");
        Console.WriteLine("You may now run NetSpot.exe");
        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }
    
    private void PatchLicense(ModuleDefMD module)
    {
        foreach (var type in module.Types)
        {
            if (type.Name == "License")
            {
                foreach (var method in type.Methods)
                {
                    if (method.Name == "get_Type")
                    {
                        var instructions = method.Body.Instructions;
                        foreach (var instruction in instructions.ToList())
                        {
                            switch (instruction.OpCode.Code)
                            {
                                case Code.Ldarg_0:
                                    Console.WriteLine("Found ldarg.0");
                                    instructions.Remove(instruction);
                                    instructions.Insert(0, OpCodes.Ldc_I4_6.ToInstruction());
                                    break;
                                case Code.Ldfld:
                                    Console.WriteLine("Found ldfld");
                                    instructions.Remove(instruction);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void PatchAboutForm(ModuleDefMD module)
    {
        foreach (var type in module.Types)
        {
            if (type.Name == "AboutForm")
            {
                foreach (var method in type.Methods)
                {
                    if (method.Name == ".ctor")
                    {
                        var instructions = method.Body.Instructions;
                        foreach (var instruction in instructions.ToList())
                        {
                            switch (instruction.OpCode.Code)
                            {
                                case Code.Call:
                                    if (instruction.Operand.ToString().Contains("NetSpot.Localization.Lang::get_GLOB_PRODUCT_NAME()")
                                        && instructions[instructions.IndexOf(instruction) - 1].Operand.ToString().Contains("NetSpot.Localization.Lang::get_ABT_PRODUCT()"))
                                    {
                                        Console.WriteLine("Found call to get_GLOB_PRODUCT_NAME");
                                        instructions[instructions.IndexOf(instruction)].OpCode = OpCodes.Nop;
                                        instructions.Insert(instructions.IndexOf(instruction) + 1, OpCodes.Ldstr.ToInstruction("NetSpot Cracked By Tana"));
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void PatchIntegrityCheck(ModuleDefMD module)
    {
        foreach (var type in module.Types)
        {
            if (type.Name == "DsProtector")
            {
                foreach (var method in type.Methods)
                {
                    if (method.Name == "CheckForDigitalSignatures")
                    {
                        var instructions = method.Body.Instructions;
                        foreach (var instruction in instructions.ToList())
                        {
                            if (instruction.OpCode.Code == Code.Ldloc_0 && instructions[instructions.IndexOf(instruction) + 1].OpCode.Code == Code.Ret)
                            {
                                Console.WriteLine("Found ldloc.0 before ret");
                                instructions[instructions.IndexOf(instruction)].OpCode = OpCodes.Nop;
                                instructions.Insert(instructions.IndexOf(instruction) + 1, OpCodes.Ldc_I4_1.ToInstruction());
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void SaveModule(ModuleDefMD module, string file)
    {
        try
        {
            var options = new ModuleWriterOptions(module);
            options.Logger = DummyLogger.NoThrowInstance;
            module.Write(file, options);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }

    private String AutoFindFile()
    {
        try {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Apps\2.0";
            var pattern = "^[A-Z0-9]{8}\\.[A-Z0-9]{3}$";
            var dir = Directory.GetDirectories(path).Where(d => Regex.IsMatch(Path.GetFileName(d), pattern)).First();
            var dir2 = Directory.GetDirectories(dir).Where(d => Regex.IsMatch(Path.GetFileName(d), pattern)).First();
            var finalDir = Directory.GetDirectories(dir2).Where(d => Regex.IsMatch(Path.GetFileName(d), "tion")).First();
            
            Console.WriteLine("Found NetSpot.exe at " + finalDir);
            return finalDir;
        } catch (Exception e) {
            Console.WriteLine("Error: " + e.Message);
            Console.Write("Couldn't automatically find the path, please enter the path to the NetSpot.exe file: ");
            return Console.ReadLine();
        }
    }
    
    private void BackupFiles(String path)
    {
        try
        {
            File.Copy(path + @"\NetSpot.exe", path + @"\NetSpot.Backup.exe", true);
            File.Copy(path + @"\NetSpot.Core.Policies.dll", path + @"\NetSpot.Core.Policies.Backup.dll", true);
        
            Console.WriteLine("Files backed up");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}