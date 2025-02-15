﻿/*
   Copyright 2012-2023 Marco De Salvo

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/


using RDFSharp.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace RDFSharp.Query
{
    /// <summary>
    /// RDFSumAggregator represents a SUM aggregation function applied by a GroupBy modifier
    /// </summary>
    public class RDFSumAggregator : RDFAggregator
    {
        #region Ctors
        /// <summary>
        /// Default-ctor to build a SUM aggregator on the given variable and with the given projection name
        /// </summary>
        public RDFSumAggregator(RDFVariable aggrVariable, RDFVariable projVariable) : base(aggrVariable, projVariable) { }
        #endregion

        #region Interfaces
        /// <summary>
        /// Gets the string representation of the SUM aggregator
        /// </summary>
        public override string ToString()
            => IsDistinct ? string.Format("(SUM(DISTINCT {0}) AS {1})", AggregatorVariable, ProjectionVariable)
                               : string.Format("(SUM({0}) AS {1})", AggregatorVariable, ProjectionVariable);
        #endregion

        #region Methods
        /// <summary>
        /// Executes the partition on the given tablerow
        /// </summary>
        internal override void ExecutePartition(string partitionKey, DataRow tableRow)
        {
            //Get row value
            double rowValue = GetRowValueAsNumber(tableRow);
            if (IsDistinct)
            {
                //Cache-Hit: distinctness failed
                if (AggregatorContext.CheckPartitionKeyRowValueCache<double>(partitionKey, rowValue))
                    return;
                //Cache-Miss: distinctness passed
                else
                    AggregatorContext.UpdatePartitionKeyRowValueCache<double>(partitionKey, rowValue);
            }
            //Get aggregator value
            double aggregatorValue = AggregatorContext.GetPartitionKeyExecutionResult<double>(partitionKey, 0d);
            //In case of non-numeric values, consider partitioning failed
            double newAggregatorValue = double.NaN;
            if (!aggregatorValue.Equals(double.NaN) && !rowValue.Equals(double.NaN))
                newAggregatorValue = rowValue + aggregatorValue;
            //Update aggregator context (sum)
            AggregatorContext.UpdatePartitionKeyExecutionResult<double>(partitionKey, newAggregatorValue);
        }

        /// <summary>
        /// Executes the projection producing result's table
        /// </summary>
        internal override DataTable ExecuteProjection(List<RDFVariable> partitionVariables)
        {
            DataTable projFuncTable = new DataTable();

            //Initialization
            partitionVariables.ForEach(pv =>
                RDFQueryEngine.AddColumn(projFuncTable, pv.VariableName));
            RDFQueryEngine.AddColumn(projFuncTable, ProjectionVariable.VariableName);

            //Finalization
            foreach (string partitionKey in AggregatorContext.ExecutionRegistry.Keys)
            {
                //Update result's table
                UpdateProjectionTable(partitionKey, projFuncTable);
            }

            return projFuncTable;
        }

        /// <summary>
        /// Helps in finalization step by updating the projection's result table
        /// </summary>
        internal override void UpdateProjectionTable(string partitionKey, DataTable projFuncTable)
        {
            //Get bindings from context
            Dictionary<string, string> bindings = new Dictionary<string, string>();
            foreach (string pkValue in partitionKey.Split(new string[] { "§PK§" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pValues = pkValue.Split(new string[] { "§PV§" }, StringSplitOptions.None);
                bindings.Add(pValues[0], pValues[1]);
            }

            //Add aggregator value to bindings
            double aggregatorValue = AggregatorContext.GetPartitionKeyExecutionResult<double>(partitionKey, 0d);
            if (aggregatorValue.Equals(double.NaN))
                bindings.Add(ProjectionVariable.VariableName, string.Empty);
            else
                bindings.Add(ProjectionVariable.VariableName, new RDFTypedLiteral(Convert.ToString(aggregatorValue, CultureInfo.InvariantCulture), RDFModelEnums.RDFDatatypes.XSD_DOUBLE).ToString());

            //Add bindings to result's table
            RDFQueryEngine.AddRow(projFuncTable, bindings);
        }
        #endregion
    }
}