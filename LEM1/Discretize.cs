using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    public class Discretize
    {
        public DataTable SourceData { get; set; }
        public Discretize(DataTable data)
        {
            this.SourceData = data;
        }

        public DataTable Discretization()
        {
            DataTable rsltData = SourceData.Copy();
            rsltData =  CheckAndParseNumericColumns(rsltData);
            rsltData = FindAndUpdateCutpoints(rsltData);
            return rsltData;
        }

        private DataTable FindAndUpdateCutpoints(DataTable data)
        {
            var rsltData = data.Copy();
            foreach (DataColumn colmn in data.Columns)
            {
                if(colmn.DataType == typeof(float))
                {
                    List<float> cutPoints = new List<float>();

                    var dstnctValues = data.AsEnumerable().Select(t => t.Field<float>(colmn.ColumnName)).
                        Distinct().OfType<float>().OrderBy(t => t).ToList();

                    float minValue = dstnctValues.Min();
                    float maxValue = dstnctValues.Max();

                    List<float> temp = new List<float>();

                    int indx = rsltData.Columns.IndexOf(colmn.ColumnName);
                    for (int i = 0; i < dstnctValues.Count-1; i++)
                    {
                        var cutPoint = (dstnctValues[i] + dstnctValues[i + 1]) / 2;
                        DataColumn tempColmn = new DataColumn(colmn.ColumnName + cutPoint,typeof(string));
                        rsltData.Columns.Add(tempColmn);
                        tempColmn.SetOrdinal(indx+ i +1);
                        foreach (DataRow row in rsltData.Rows)
                        {
                            if ((float)row[colmn.ColumnName] < cutPoint)
                                row[tempColmn] = string.Format("{0}..{1}", minValue, cutPoint);
                            else
                                row[tempColmn] = string.Format("{0}..{1}", cutPoint, maxValue);
                        }

                    }
                    rsltData.Columns.RemoveAt(indx);
                }
            }
            return rsltData;
        }
        private DataTable CheckAndParseNumericColumns(DataTable data)
        {
            var rsltData = data.Copy();
            foreach (DataColumn item in data.Columns)
            {
                if(item.Ordinal < data.Columns.Count-2)
                {
                    List<DataColumn> tempColns = new List<DataColumn>();
                    List<float> values = new List<float>();
                    var t = data.AsEnumerable().ToList();
                    for (int i = 0; i < t.Count; i++)
                    {
                        if (float.TryParse((string)t[i][item.ColumnName], out float val))
                            values.Add(val);
                        else
                            return rsltData;
                    }
                    //data.AsEnumerable()
                    //    .ToList().ForEach(r =>
                    //    {
                    //        if (float.TryParse(r.Field<string>(item.ColumnName), out float i))
                    //            values.Add(i);
                    //    });
                    if (values.Count == data.Rows.Count)
                    {
                        var indx = data.Columns.IndexOf(item);
                        DataColumn tempCol = new DataColumn(item.ColumnName, typeof(float));
                        rsltData.Columns.RemoveAt(indx);
                        rsltData.Columns.Add(tempCol);
                        tempCol.SetOrdinal(indx);
                        for (int i = 0; i < data.Rows.Count; i++)
                        {
                            rsltData.Rows[i][indx] = values[i];
                        }
                    }
                }
            }

            return rsltData;
        }
    }
}
