using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RowAC
{
    class RLog
    {
        object lockobj = new object();
        string prefix = "";
        string logsFolder = "";

        public RLog(string prefix, string folder)
        {
            this.prefix = prefix;
            UpdateLogsFolder(folder);
        }

        public void UpdatePrefix(string prefix)
        {
            this.prefix = prefix;
        }

        public void UpdateLogsFolder(string folder)
        {
            logsFolder = folder;
            try
            {
                if (IsValidFoldername(logsFolder) && !string.IsNullOrEmpty(logsFolder))
                    Directory.CreateDirectory(logsFolder);
                return;
            }
            catch (ArgumentException ex) { Console.WriteLine("Not valid path! Now path is root folder."); }
            logsFolder = "";
        }

        public void LogEx(string exPrefix, Exception exception)
        {
            LogEx(exPrefix, exception.ToString());
        }

        public void LogEx(string exPrefix, string exception)
        {
            Log(string.Format("==[{0}EX]: {1}", exPrefix, exception));
        }

        public void Log(string message, bool alert = true)
        {
            try
            {
                message = string.Format("[{0}][{1}] {2}", prefix, DateTime.Now.ToString("HH:mm:ss"), message);
                if (alert)
                {
                    UnityEngine.Debug.Log(message);
                    Console.WriteLine(message);
                }

                // Write to file new line
                string tempPrefix = prefix;
                if (!IsValidFilename(tempPrefix))
                    foreach (var ch in Path.GetInvalidFileNameChars())
                        tempPrefix = tempPrefix.Replace(ch.ToString(), "");

                string path = Path.Combine(logsFolder, tempPrefix + "_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt");
                lock (lockobj)
                using (StreamWriter writer = new StreamWriter(path, true))
                    writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        bool IsValidFilename(string fileName)
        {
            string regexString = "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]";
            Regex containsABadCharacter = new Regex(regexString);

            if (containsABadCharacter.IsMatch(fileName))
                return false;
            return true;
        }

        bool IsValidFoldername(string folderName)
        {
            string regexString = "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]";
            Regex containsABadCharacter = new Regex(regexString);

            if (containsABadCharacter.IsMatch(folderName))
                return false;
            return true;
        }

        internal static void Tests()
        {
            RLog rlog = new RLog("rlogTests", "");
            try
            {
                RLog log = new RLog("[Test1]", "");
                log.Log("test1");
                Console.WriteLine("===");
                log = new RLog("[><Test2.!?:|]", "");
                log.Log("test2");
                Console.WriteLine("===");
                log = new RLog("[Test3]", ".\\NoSuchFolder");
                log.Log("test3");
                Console.WriteLine("===");
                log = new RLog("[Test4]", ".\\NoSuchFolder\\Nonono");
                log.Log("test4");
                Console.WriteLine("===");
                log = new RLog("[Test5]", ".\\NoSuchFolder\\No$uch4ol!?:der");
                log.Log("test5");
                Console.WriteLine("===");

                rlog.Log("OK");
            }
            catch (Exception ex) { rlog.Log("FAIL: " + ex.ToString()); }
            rlog.Log("END");
        }
    }
}
