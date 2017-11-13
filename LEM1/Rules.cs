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
        public DataTable SourceData { get; set; }
        public Dictionary<string,List<string>> SingleCovering { get; set; }

        private List<List<string>> aStar = new List<List<string>>();
        private Dictionary<string, List<string>> dStar = new Dictionary<string, List<string>>();

        private Dictionary<string, List<string>> lowerApprox = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> upperApprox = new Dictionary<string, List<string>>();

        public Rules(DataTable data)
        {
            this.SourceData = data;
            SingleCovering = new Dictionary<string, List<string>>();
        }
        public bool CheckInitialCondition()
        {
            bool intlCondition = true;
            GetDecisions(SourceData);
            if (CheckAStarLessThanDStar(SourceData))
            {
                Console.WriteLine("A*<=d*");
            }
            else
            {
                intlCondition = false;
            }
            FindApproximations();
            return intlCondition;
        }
        public void ComputeSingleGlobalCovering()
        {
            var colCount = SourceData.Columns.Count - 1;
            foreach (string concept in SourceData.AsEnumerable().Select(t => t[colCount - 1]).Distinct().ToList())
            {
                var conceptData = SourceData.Copy();
                //Update Decision Values to Naresh except for current concept
                conceptData.AsEnumerable().Where(row => row.Field<string>(colCount - 1) != concept).ToList().
                    ForEach(v => v.SetField<string>(colCount - 1, "NARESH"));
                GetNextValidSets(conceptData);
            }
            int f = 354234;
        }
        private void GetNextValidSets(DataTable data)
        {
            var colCount = data.Columns.Count - 1;
            if (colCount == 3)
                return;
            for (int i = 0; i <= colCount-2; i++)
            {
                var tempData = data.Copy();
                tempData.Columns.RemoveAt(i);
                tempData.AcceptChanges();
                var tempColCount = tempData.Columns.Count - 1;
                if (CheckAStarLessThanDStar(tempData))
                {
                    var tempRule = tempData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList()
                        .GetRange(0, tempData.Columns.Count - 2);
                    var conceptName =tempData.AsEnumerable().Select(t => t[tempColCount - 1]).Distinct().ToList()
                        .OfType<string>().Where(v=>v!="NARESH").FirstOrDefault();

                    if (SingleCovering.ContainsKey(conceptName))
                        SingleCovering.Remove(conceptName);

                    SingleCovering.Add(conceptName, tempRule);
                    GetNextValidSets(tempData);
                    break;
                }
            }
            return;
        }
        private void FindApproximations()
        {
            foreach (var concept in dStar)
            {
                List<string> tempLowList = new List<string>();
                List<string> tempHighList = new List<string>();
                foreach (var sets in aStar)
                {
                    var intersectCnt = concept.Value.Intersect(sets).Count();
                    if (intersectCnt == sets.Count())
                        tempLowList.AddRange(sets);
                    if(intersectCnt>0)
                        tempHighList.AddRange(sets);
                }
                lowerApprox.Add(concept.Key, tempLowList);
                upperApprox.Add(concept.Key, tempHighList);
            }
        }
        private bool CheckAStarLessThanDStar(DataTable data)
        {
            DataTable temp = data.Copy();
            IEqualityComparer<DataRow> comparer = new RowChecker();
            var colCount = data.Columns.Count-1;
            bool intlCndtn = true;

            //Retrieve AStar
            foreach (var row in data.AsEnumerable().Distinct(comparer))
            {
                var same = temp.AsEnumerable().Where(t => comparer.Equals(t, row)).Select(t => t.Field<string>("ID")).ToList();
                if (same.Count > 0)
                    this.aStar.Add(same);
            }
            //Check A*<=d*
            foreach (var sets in aStar)
            {
                var diff = data.AsEnumerable().Where(row => sets.Exists(val => val == row.Field<string>(colCount))).
                    Select(decisionName => decisionName.Field<string>(colCount - 1)).Distinct().ToList();
                if (diff.Count > 1)
                {
                    intlCndtn = false;
                    break;
                }
            }
            return intlCndtn;
        }
        private void GetDecisions(DataTable data)
        {
            //var temp = from t in data.AsEnumerable()
            //           select new
            //           {
            //               Decision = t.Field<int>(t.Table.Columns.Count - 1),
            //               ID = t.Field<int>(t.Table.Columns.Count)
            //           };
            var colCount= data.Columns.Count - 1;
            foreach (string conceptName in data.AsEnumerable().Select(t=>t[colCount-1]).Distinct().ToList())
            {
                var same = data.AsEnumerable().Where(t=>t.Field<string>(colCount-1) == conceptName).Select(t => t.Field<string>("ID")).ToList();
                if (same.Count > 0)
                    dStar.Add(conceptName,same);
            }
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
