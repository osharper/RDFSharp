﻿/*
   Copyright 2012-2022 Marco De Salvo

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
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace RDFSharp.Query
{
    /// <summary>
    /// RDFMathExpression represents an arithmetical expression to be applied on a query results table.
    /// </summary>
    public class RDFUnaryExpression : RDFExpression
    {
        #region Ctors
        /// <summary>
        /// Default-ctor to build an unary expression with given arguments
        /// </summary>
        public RDFUnaryExpression(RDFExpression leftArgument) 
            : base(leftArgument, null as RDFExpression) { }

        /// <summary>
        /// Default-ctor to build an unary expression with given arguments
        /// </summary>
        public RDFUnaryExpression(RDFVariable leftArgument)
            : base(leftArgument, null as RDFExpression) { }

        /// <summary>
        /// Default-ctor to build an unary expression with given arguments
        /// </summary>
        public RDFUnaryExpression(RDFResource leftArgument)
            : base(leftArgument, null as RDFExpression) { }

        /// <summary>
        /// Default-ctor to build an unary expression with given arguments
        /// </summary>
        public RDFUnaryExpression(RDFLiteral leftArgument)
            : base(leftArgument, null as RDFExpression) { }
        #endregion

        #region Interfaces
        /// <summary>
        /// Gives the string representation of the unary addition expression
        /// </summary>
        public override string ToString()
            => this.ToString(new List<RDFNamespace>());
        internal override string ToString(List<RDFNamespace> prefixes)
        {
            StringBuilder sb = new StringBuilder();

            //(L)
            sb.Append('(');
            sb.Append(LeftArgument is RDFExpression expLeftArgument ? expLeftArgument.ToString(prefixes)
                                                                    : RDFQueryPrinter.PrintPatternMember((RDFPatternMember)LeftArgument, prefixes));
            sb.Append(')');

            return sb.ToString();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Applies the unary expression on the given datarow
        /// </summary>
        internal override RDFPatternMember ApplyExpression(DataRow row)
        {
            RDFPatternMember expressionResult = null;

            #region Guards
            if (LeftArgument is RDFVariable && !row.Table.Columns.Contains(LeftArgument.ToString()))
                return expressionResult;
            #endregion

            try
            {
                #region Evaluate Arguments
                //Evaluate left argument (Expression VS Variable VS Resource/Literal)
                RDFPatternMember leftArgumentPMember = null;
                if (LeftArgument is RDFExpression leftArgumentExpression)
                    leftArgumentPMember = leftArgumentExpression.ApplyExpression(row);
                else if (LeftArgument is RDFVariable)
                    leftArgumentPMember = RDFQueryUtilities.ParseRDFPatternMember(row[LeftArgument.ToString()].ToString());
                else
                    leftArgumentPMember = (RDFPatternMember)LeftArgument;
                #endregion

                #region Calculate Result
                expressionResult = leftArgumentPMember;
                #endregion
            }
            catch { /* Just a no-op, since type errors are normal when trying to face variable's bindings */ }

            return expressionResult;
        }
        #endregion
    }
}