using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    public enum RuleSet
    {
        Certain,
        Possible
    }
    public class Rules
    {
        public DataTable SourceData { get; set; }
        public Dictionary<string,List<string>> certainRules { get; set; }
        public Dictionary<string, List<string>> possibleRules { get; set; }

        private List<List<string>> aStar = new List<List<string>>();
        private Dictionary<string, List<string>> dStar = new Dictionary<string, List<string>>();

        private Dictionary<string, List<string>> lowerApprox = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> upperApprox = new Dictionary<string, List<string>>();

        public Rules(DataTable data)
        {
            this.SourceData = data;
            certainRules = new Dictionary<string, List<string>>();
            possibleRules = new Dictionary<string, List<string>>();
        }
        public bool CheckInitialCondition()
        {
            bool intlCondition = true;
            GetDecisions(SourceData);
            this.aStar = ComputeAStar(SourceData);
            if (CheckAStarLessThanDStar(SourceData))
            {
                Console.WriteLine("A*<=d*");
            }
            else
            {
                Console.WriteLine("A*!<=d*");
                intlCondition = false;
            }
            FindApproximations();
            return intlCondition;
        }

        public void ComputeSingleGlobalCovering()
        {
            var colCount = SourceData.Columns.Count - 1;
            foreach (var lower in lowerApprox)
            {
                KeyValuePair<string, List<string>> upper = new KeyValuePair<string, List<string>>();
                if (upperApprox.ContainsKey(lower.Key))
                    upper = upperApprox.Where(t => t.Key == lower.Key).FirstOrDefault();

                //Parallel.Invoke(
                //    () => 
                //    {
                //ComputeCovering(SourceData, lower,lowerSingleCovering);
                //},
                //() => 
                //{
                ComputeCovering(SourceData, upper, RuleSet.Possible);
                //}
                //);
            }
        }

        private List<List<string>> ComputeAStar(DataTable sourceData)
        {
            DataTable temp = sourceData.Copy();
            IEqualityComparer<DataRow> comparer = new RowChecker();
            var colCount = sourceData.Columns.Count - 1;
            List<List<string>> tempAStar = new List<List<string>>();
            //Retrieve AStar
            foreach (var row in sourceData.AsEnumerable().Distinct(comparer))
            {
                var same = temp.AsEnumerable().Where(t => comparer.Equals(t, row)).Select(t => t.Field<string>("ID")).ToList();
                if (same.Count > 0)
                    tempAStar.Add(same);
            }
            return tempAStar;
        }

        private void ComputeCovering(DataTable data,KeyValuePair<string,List<string>> concept,RuleSet ruleType)
        {
            if (concept.Value == null || !concept.Value.Any())
                return;
            var conceptData = data.Copy();
            var colCount = data.Columns.Count - 1;
            //Update Decision Values to Naresh except for current concept
            var temp = conceptData.AsEnumerable().Where(row => concept.Value.Exists(val => val == row.Field<string>(colCount - 0))).ToList();

            if (ruleType == RuleSet.Possible)
            {
                var te= temp.AsEnumerable().Where(row => row.Field<string>(colCount - 1) != concept.Key).ToList();
                te.ForEach(t => temp.Remove(t));
            }

            conceptData.AsEnumerable().Except(temp).ToList().
                ForEach(v => v.SetField(colCount - 1, "NARESH"));
            GetNextValidSets(conceptData, ruleType);
        }
        private void GetNextValidSets(DataTable data, RuleSet ruleType)
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
                    if(RuleSet.Certain == ruleType)
                    {
                        if (certainRules.ContainsKey(conceptName))
                            certainRules.Remove(conceptName);

                        certainRules.Add(conceptName, tempRule);
                        GetNextValidSets(tempData, RuleSet.Certain);
                    }
                    else
                    {
                        if (possibleRules.ContainsKey(conceptName))
                            possibleRules.Remove(conceptName);

                        possibleRules.Add(conceptName, tempRule);
                        GetNextValidSets(tempData, RuleSet.Possible);
                    }
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
            IEqualityComparer<DataRow> comparer = new RowChecker();
            var colCount = data.Columns.Count-1;
            bool intlCndtn = true;

            //Retrieve AStar
            var tempAstar =  ComputeAStar(data);
            //Check A*<=d*
            foreach (var sets in tempAstar)
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
