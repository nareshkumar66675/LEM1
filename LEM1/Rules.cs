using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEM1
{
    public enum RuleType
    {
        Certain,
        Possible
    }
    public class Rules
    {
        public DataTable SourceData { get; set; }
        public Dictionary<string,List<string>> CertainRules { get; set; }
        public Dictionary<string, List<string>> PossibleRules { get; set; }

        public bool IsConsistent { get; set; }

        public Dictionary<string,List<Dictionary<string,string>>> CertainRuleSet { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> PossibleRuleSet { get; set; }

        private List<List<string>> aStar = new List<List<string>>();
        private Dictionary<string, List<string>> dStar = new Dictionary<string, List<string>>();

        private Dictionary<string, List<string>> lowerApprox = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> upperApprox = new Dictionary<string, List<string>>();
        

        public Rules(DataTable data)
        {
            this.SourceData = data;
            CertainRules = new Dictionary<string, List<string>>();
            PossibleRules = new Dictionary<string, List<string>>();
            CertainRuleSet = new Dictionary<string, List<Dictionary<string, string>>>();
            PossibleRuleSet = new Dictionary<string, List<Dictionary<string, string>>>();
        }
        public bool CheckInitialCondition()
        {
            IsConsistent = true;
            GetDecisions(SourceData);
            this.aStar = ComputeAStar(SourceData);
            if (CheckAStarLessThanDStar(SourceData))
            {
                Console.WriteLine("A*<=d*");
            }
            else
            {
                Console.WriteLine("A*!<=d*");
                IsConsistent = false;
            }
            FindApproximations();
            return IsConsistent;
        }

        public void ComputeSingleGlobalCovering()
        {
            var colCount = SourceData.Columns.Count - 1;
            foreach (var lower in lowerApprox)
            {
                KeyValuePair<string, List<string>> upper = new KeyValuePair<string, List<string>>();

                if (upperApprox.ContainsKey(lower.Key))
                    upper = upperApprox.Where(t => t.Key == lower.Key).FirstOrDefault();

                Parallel.Invoke(() =>
                {
                    ComputeCovering(SourceData, lower, RuleType.Certain);
                },
                () =>
                {
                    if(!IsConsistent) 
                        ComputeCovering(SourceData, upper, RuleType.Possible);
                });
            }
            Parallel.Invoke(()=>
            {
                ComputeRuleSetAndDrop(RuleType.Certain);
            },
            ()=>
            {
                if(!IsConsistent)
                    ComputeRuleSetAndDrop(RuleType.Possible);
            });
        }

        public string GetRuleSet(RuleType ruleType)
        {
            Dictionary<string, List<Dictionary<string, string>>> tempSet = new Dictionary<string, List<Dictionary<string, string>>>();

            switch (ruleType)
            {
                case RuleType.Certain:
                    tempSet = CertainRuleSet;
                    break;
                case RuleType.Possible:
                    tempSet = PossibleRuleSet;
                    break;
                default:
                    break;
            }
            List<string> rsltList = new List<string>();
            var dcnName =SourceData.Columns[SourceData.Columns.Count - 2].ColumnName;
            foreach (var rule in tempSet)
            {
                foreach (var item in rule.Value)
                {
                    var last = item.Last();
                    StringBuilder str = new StringBuilder();
                    foreach (var attr in item)
                    {
                        if (attr.Key == last.Key)
                        {
                            str.AppendLine(string.Format("({0}, {1}) -> ({2}, {3})", attr.Key, attr.Value, dcnName, rule.Key));
                        }
                        else
                            str.AppendFormat("({0}, {1}) & ", attr.Key, attr.Value);
                    }
                    rsltList.Add(str.ToString());
                }
            }
            return string.Join("",rsltList.Distinct().ToArray());
        }

        private List<List<string>> ComputeAStar(DataTable sourceData)
        {
            //DataTable temp = sourceData.Copy();
            IEqualityComparer<DataRow> comparer = new RowChecker();
            var colCount = sourceData.Columns.Count - 1;
            
            List<List<string>> tempAStar = new List<List<string>>();
            List<string> removedSets = new List<string>();
            var dat = sourceData.AsEnumerable();
            //Retrieve AStar
            foreach (var row in dat.Distinct(comparer))
            {
                var same = dat.Where(t => comparer.Equals(t, row)).Select(t => t.Field<string>("ID")).ToList();
                //removedSets.AddRange(same);
                if (same.Count > 0)
                    tempAStar.Add(same);
                
            }
            return tempAStar;
        }

        private void ComputeCovering(DataTable data,KeyValuePair<string,List<string>> concept,RuleType ruleType)
        {
            if (concept.Value == null || !concept.Value.Any())
                return;
            var conceptData = data.Copy();
            var colCount = data.Columns.Count - 1;
            //Update Decision Values to Naresh except for current concept
            var temp = conceptData.AsEnumerable().Where(row => concept.Value.Exists(val => val == row.Field<string>(colCount - 0))).ToList();

            if (ruleType == RuleType.Possible)
            {
                var te= temp.AsEnumerable().Where(row => row.Field<string>(colCount - 1) != concept.Key).ToList();
                te.ForEach(t => temp.Remove(t));
            }

            conceptData.AsEnumerable().Except(temp).ToList().
                ForEach(v => v.SetField(colCount - 1, "NARESH"));
            GetNextValidSets(conceptData,concept, ruleType);
        }
        private void GetNextValidSets(DataTable data, KeyValuePair<string, List<string>> concept, RuleType ruleType)
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
                
                if (CheckAStarLessThanDApprox(tempData,concept))
                {
                    var tempRule = tempData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList()
                        .GetRange(0, tempData.Columns.Count - 2);
                    var conceptName = concept.Key; //tempData.AsEnumerable().Select(t => t[tempColCount - 1]).Distinct().ToList()
                        //.OfType<string>().Where(v=>v!="NARESH").FirstOrDefault();
                    //CheckAStarLessThanDApprox(tempData, concept);
                    if (RuleType.Certain == ruleType)
                    {
                        if (CertainRules.ContainsKey(conceptName))
                            CertainRules.Remove(conceptName);

                        CertainRules.Add(conceptName, tempRule);
                        GetNextValidSets(tempData,concept, RuleType.Certain);
                    }
                    else
                    {
                        if (PossibleRules.ContainsKey(conceptName))
                            PossibleRules.Remove(conceptName);

                        PossibleRules.Add(conceptName, tempRule);
                        GetNextValidSets(tempData,concept, RuleType.Possible);
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
        private bool CheckAStarLessThanDApprox(DataTable data, KeyValuePair<string, List<string>> concept)
        {
            var tempAStar=ComputeAStar(data);

            foreach (var set in tempAStar)
            {
                if (set.Except(concept.Value).Any() && set.Intersect(concept.Value).Any())
                    return false;
            }

            return true;
        }
        private bool CheckAStarLessThanDStar(DataTable data)
        {
            IEqualityComparer<DataRow> comparer = new RowChecker();
            var colCount = data.Columns.Count-1;
            bool intlCndtn = true;
            
            //Check A*<=d*
            foreach (var sets in this.aStar)
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
            var colCount= data.Columns.Count - 1;
            foreach (string conceptName in data.AsEnumerable().Select(t=>t[colCount-1]).Distinct().ToList())
            {
                var same = data.AsEnumerable().Where(t=>t.Field<string>(colCount-1) == conceptName).Select(t => t.Field<string>("ID")).ToList();
                if (same.Count > 0)
                    dStar.Add(conceptName,same);
            }
        }
        
        private void ComputeRuleSetAndDrop(RuleType ruleType)
        {
            if (ruleType == RuleType.Certain)
            {
                CertainRuleSet = ComputeRuleSet(CertainRules, ruleType);
                DropConditions(CertainRuleSet);
            }
            else
            {
                PossibleRuleSet = ComputeRuleSet(PossibleRules, ruleType);
                DropConditions(PossibleRuleSet);
            }
        }
        private void DropConditions(Dictionary<string, List<Dictionary<string, string>>> globalRuleSet)
        {        
            foreach (var rulSet in globalRuleSet)
            {
                var tep = new List<Dictionary<string, string>>(rulSet.Value);
                for (int i = 0; i < rulSet.Value.Count; i++)
                {
                    var value = CheckDroppings(new KeyValuePair<string, Dictionary<string, string>>(rulSet.Key, rulSet.Value[i]));
                    if (value != null)
                        rulSet.Value[i] = value;
                }
            }
        }
        private Dictionary<string, string> CheckDroppings(KeyValuePair<string,Dictionary<string,string>> rule)
        {
            var last = rule.Value.Last();
            StringBuilder qryCndtn = new StringBuilder();
            foreach (var colData in rule.Value)
            {
                if (colData.Key == last.Key)
                    qryCndtn.AppendFormat("[{0}] = '{1}'", colData.Key, colData.Value);
                else
                    qryCndtn.AppendFormat("[{0}] = '{1}' AND ", colData.Key, colData.Value);
            }
            var dcnCnt =SourceData.Select(qryCndtn.ToString()).AsEnumerable().Select(t => new { decision = t.Field<string>(t.Table.Columns.Count - 2) }).Distinct().Count();

            if (dcnCnt > 1)
                return null;

            if (rule.Value.Count > 1)
            {
                Dictionary<string, string> temp = new Dictionary<string, string>();
                for (int i = 0; i < rule.Value.Count; i++)
                {
                    var tempDict = new Dictionary<string, string>(rule.Value);
                    tempDict.Remove(rule.Value.ElementAt(i).Key);
                    var subRulSet = CheckDroppings(new KeyValuePair<string, Dictionary<string, string>>(rule.Key, tempDict));
                    if (subRulSet == null)
                    {
                        if (i == rule.Value.Count - 1)
                            return rule.Value;
                        else
                            continue;
                    }
                    else if (subRulSet.Count < rule.Value.Count)
                        return subRulSet;
                }
            }
            else
                return rule.Value;
            return null;

        }
        private Dictionary<string, List<Dictionary<string, string>>> ComputeRuleSet(Dictionary<string, List<string>> rules, RuleType ruleType)
        {
            Dictionary<string, List<Dictionary<string, string>>> tempRuleSet = new Dictionary<string, List<Dictionary<string, string>>>();
            foreach (var rul in rules)
            {
                List<string> ids = new List<string>();
                if (ruleType==RuleType.Certain)
                    lowerApprox.TryGetValue(rul.Key, out ids);
                else
                    upperApprox.TryGetValue(rul.Key, out ids);

                var data = SourceData.Copy().AsEnumerable().Where(t => ids.Contains(t.Field<string>("ID"))).CopyToDataTable();

                for (int i = 0; i < SourceData.Columns.Count; i++)
                {
                    if (!rul.Value.Contains(SourceData.Columns[i].ColumnName))
                    {
                        data.Columns.Remove(SourceData.Columns[i].ColumnName);
                    }
                }

                var distinct = data.AsEnumerable().Distinct(DataRowComparer.Default);
                List<Dictionary<string, string>> tempRulesetList = new List<Dictionary<string, string>>();
                foreach (DataRow rows in distinct)
                {
                    Dictionary<string, string> tempSet = new Dictionary<string, string>();
                    foreach (DataColumn col in data.Columns)
                    {
                        tempSet.Add(col.ColumnName, (string)rows[col]);
                    }
                    tempRulesetList.Add(tempSet);
                }
                tempRuleSet.Add(rul.Key, tempRulesetList);
            }
            return tempRuleSet;
        }
    }

    public class RowChecker : EqualityComparer<DataRow>
    {
        public override bool Equals(DataRow row1, DataRow row2)
        {
            int cnt = row1.Table.Columns.Count;
            for (int i = 0; i < cnt-2; i++)
            {
                if ((string)row1[i] != (string)row2[i])
                    return false;
            }
            return true;
            //var value1 = row1.ItemArray.ToList();
            //var value2 = row2.ItemArray.ToList();
            
            //return value1.GetRange(0, value1.Count - 2).SequenceEqual(value2.GetRange(0, value2.Count - 2));
        }
        public override int GetHashCode(DataRow obj)
        {
            return base.GetHashCode();
        }
    }
}
