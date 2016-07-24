using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RowPatcher
{
    class Program
    {
        private static AssemblyDefinition rustAssembly = null;
        private static AssemblyDefinition rowacAssembly = null;
        private static TypeDefinition hooksType = null;

        static void Main()
        {
            Console.WriteLine("RowPatcher " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("Need to patch client or server?\n1. Legacy client\n2. Legacy server");
            int mode = 0;
            string answer = Console.ReadLine();

            if (!int.TryParse(answer, out mode))
            {
                Console.Clear();
                Main();
            }

            try
            {
                rustAssembly = AssemblyDefinition.ReadAssembly("Assembly-CSharp.dll");

                if (mode == 1)
                {
                    rowacAssembly = AssemblyDefinition.ReadAssembly("RGuard.dll");
                    ClientBootstrapAttachPatch();
                    Console.WriteLine("Client patched");
                }
                else if (mode == 2)
                {
                    rowacAssembly = AssemblyDefinition.ReadAssembly("RowAC.dll");
                    hooksType = rowacAssembly.MainModule.GetType("RowAC", "Hooks");

                    BootstrapAttachPatch();
                    //PlayerSpawnHookPatch();
                    Console.WriteLine("Server patched");
                }
                rustAssembly.Write("Assembly-CSharp.dll");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("END");
            Console.ReadLine();
        }

        // TODO: update hook
        private static void PlayerSpawnHookPatch()
        {
            TypeDefinition MainType = rustAssembly.MainModule.GetType("BasePlayer");
            MethodDefinition MainMethod = null;
            foreach (var Method in MainType.Methods)
                if (Method.Name == "Respawn")
                {
                    MainMethod = Method;
                    break;
                }

            MethodDefinition HookMethod = null;
            foreach (var Method in hooksType.Methods)
                if (Method.Name == "PlayerSpawn")
                {
                    HookMethod = Method;
                    break;
                }

            int Position = MainMethod.Body.Instructions.Count - 1;

            ILProcessor iLProcessor = MainMethod.Body.GetILProcessor();

            iLProcessor.InsertBefore(MainMethod.Body.Instructions[Position],
                Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(HookMethod)));
            iLProcessor.InsertBefore(MainMethod.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_0));
        }

        private static void BootstrapAttachPatch()
        {
            TypeDefinition ACInit = rowacAssembly.MainModule.GetType("RowAC", "Loader");
            TypeDefinition serverInit = rustAssembly.MainModule.GetType("NetCull");

            MethodDefinition attachBootstrap = null;
            foreach (var method in ACInit.Methods)
                if (method.Name == "Init")
                {
                    attachBootstrap = method;
                    break;
                }

            MethodDefinition awake = null;
            foreach (var method in serverInit.Methods)
                if (method.Name == "InitializeServer")
                {
                    awake = method;
                    break;
                }

            // 
            awake.Body.GetILProcessor().InsertBefore(awake.Body.Instructions[0], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(attachBootstrap)));
        }

        private static void ClientBootstrapAttachPatch()
        {
            TypeDefinition rowac = rowacAssembly.MainModule.GetType("Loader", "Program");
            TypeDefinition serverInit = rustAssembly.MainModule.GetType("MainMenu"); // MainMenuSystem for alpha

            MethodDefinition attachBootstrap = null;
            foreach (var method in rowac.Methods)
                if (method.Name == "Load")
                {
                    attachBootstrap = method;
                    break;
                }

            MethodDefinition awake = null;
            foreach (var method in serverInit.Methods)
                if (method.Name == "Show")
                {
                    awake = method;
                    break;
                }

            awake.Body.GetILProcessor().InsertBefore(awake.Body.Instructions[awake.Body.Instructions.Count - 1],
                Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(attachBootstrap)));
        }
    }
}