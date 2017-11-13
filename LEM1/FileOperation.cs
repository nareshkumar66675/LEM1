using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    public static class FileOperation
    {
        public static DataTable ReadDataFile(string path)
        {
            DataTable data = new DataTable();
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        switch (line[0])
                        {
                            case '<':
                             break;
                            case '!':
                                break;
                            case '[':
                                ParseHeaders(line, data);
                                break;
                            default:
                                ParseLine(line,data);
                                break;
                        }
                    }
                }

            }
            else
            {
                throw new Exception("Data File Missing - "+ Path.GetFileName(path));
            }
            return data;
        }
        private static void ParseHeaders(string line,DataTable data)
        {
            var colHeaders = line.Trim().Replace('[', ' ').Replace(']', ' ').Split(new char[0], StringSplitOptions.RemoveEmptyEntries).ToList();
            colHeaders.ForEach(t => data.Columns.Add(new DataColumn(t, typeof(string))));
            data.Columns.Add(new DataColumn("ID", typeof(string)));
        }
        private static void ParseLine(string line, DataTable data)
        {
            var values = line.Trim().Split(new char[0]).ToList();
            values.Add(data.Rows.Count + 1+"");

            if (values.Count == data.Columns.Count)
            {
                data.Rows.Add(values.ToArray());
            }
            else
                throw new Exception("Column and Data count Mismatch");

        }
    }
}
