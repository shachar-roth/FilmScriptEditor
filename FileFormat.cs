using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FilmScriptEditor
{
    internal class FileFormat
    {
        public class Version
        {
            public string Name;
            public DateTime Date;
            public string Patch;

            public static Version DeSerialize(StreamReader reader)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    return null;

                Version result = new Version();
                var splitedLine = line.Split(' ');
                result.Date = DateTime.Parse(splitedLine[0]);
                result.Name = splitedLine[1];

                StringBuilder stringBuilder = new StringBuilder();
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    stringBuilder.AppendLine(line);
                }
                result.Patch = stringBuilder.ToString();
                return result;
            }

            public void Serialize(StreamWriter writer)
            {
                writer.WriteLine(string.Join(' ', Date.ToString("s"), Name));
                writer.WriteLine(Patch);
            }
        }

        public const string Extension = ".תסריט";

        public string Current;
        public List<Version> PreviousVersions = new List<Version>();

        public void Commit(string newXaml)
        {
            if (string.IsNullOrWhiteSpace(Current))
            {
                Current = newXaml;
                return;
            }

            var patcher = new diff_match_patch();
            List<Patch> patches = patcher.patch_make(newXaml, Current);
            Version newVersion = new Version
            {
                Patch = patcher.patch_toText(patches),
                Date = DateTime.UtcNow,
            };
            PreviousVersions.Insert(0, newVersion);
            Current = newXaml;
        }

        public string GetPreviousVersion(int versionNumber)
        {
            var current = Current;
            var enumerator = PreviousVersions.GetEnumerator();
            for (int i = 0; i < versionNumber; i++)
            {
                enumerator.MoveNext();
                var patcher = new diff_match_patch();
                current = patcher.patch_apply(patcher.patch_fromText(enumerator.Current.Patch), current).Item1;
            }
            return current;
        }

        public void Serialize(StreamWriter writer)
        {
            writer.WriteLine(Current);
            writer.WriteLine();
            PreviousVersions.ForEach(version => version.Serialize(writer));
        }

        public static FileFormat DeSerialize(StreamReader reader)
        {
            FileFormat result = new FileFormat();
            if (reader.EndOfStream)
                return result;

            result.Current = reader.ReadLine();
            reader.ReadLine();

            Version nextVersion;
            while ((nextVersion = Version.DeSerialize(reader)) != null)
            {
                result.PreviousVersions.Add(nextVersion);
            }
            return result;
        }
    }
}