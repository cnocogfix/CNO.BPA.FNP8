using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CNO.BPA.FNP8
{
   public class ConditionalProperty
   {
         private string _name = String.Empty;
         private string _value = String.Empty;
         private COperator _conditionalOperator;
         private ROperator _relationalOperator;
         public enum ROperator
         {
            And,
            Or
         }
         public enum COperator
         {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            Like,
            Null,
            NotNull
         }
         public COperator ConditionalOperator
         {
            get { return _conditionalOperator; }
            set { _conditionalOperator = value; }
         }
         public ROperator RelationalOperator
         {
            get { return _relationalOperator; }
            set { _relationalOperator = value; }
         }
         public string Name
         {
            get { return _name; }
            set { _name = value; }
         }
         public string Value
         {
            get { return _value; }
            set { _value = value; }
         }

      }

   }

