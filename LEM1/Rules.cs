using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    public class Rules
    {
        public void CheckInitialCondition(DataTable data)
        {
            DataTable temp = data.Copy();
            IEqualityComparer<DataRow> comparer = new RowChecker();
            List<List<string>> aStar = new List<List<string>>();

            foreach (var row in data.AsEnumerable().Distinct(comparer))
            {
                var same = temp.AsEnumerable().Where(t => comparer.Equals(t, row)).Select(t => t.Field<string>("ID")).ToList();
                if(same.Count>0)
                    aStar.Add(same);
            }
            var tx =GetDecisions(data);
        }
        private List<List<string>> GetDecisions(DataTable data)
        {
            //var temp = from t in data.AsEnumerable()
            //           select new
            //           {
            //               Decision = t.Field<int>(t.Table.Columns.Count - 1),
            //               ID = t.Field<int>(t.Table.Columns.Count)
            //           };
            var colCount= data.Columns.Count - 1;
            List <List<string>> dStar = new List<List<string>>();
            foreach (var item in data.AsEnumerable().Select(t=>t[colCount-1]).Distinct().ToList())
            {
                var value = (string)item;
                var same = data.AsEnumerable().Where(t=>t.Field<string>(colCount-1)==value).Select(t => t.Field<string>("ID")).ToList();
                if (same.Count > 0)
                    dStar.Add(same);
            }


            return dStar;
        }

    }

    public class RowChecker : EqualityComparer<DataRow>
    {
        public override bool Equals(DataRow row1, DataRow row2)
        {

            var value1 = row1.ItemArray.ToList();
            var value2 = row2.ItemArray.ToList();

            return value1.GetRange(0, value1.Count - 2).SequenceEqual(value2.GetRange(0, value2.Count - 2));
        }

        public override int GetHashCode(DataRow obj)
        {
            return base.GetHashCode();
        }
    }
}
