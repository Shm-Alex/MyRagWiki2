using System;
using System.Reflection;
using System.Workflow.Activities.Rules;
using System.Collections;
using Framework.Collections;
using System.Collections.Generic;

namespace Ksema.ObjectModel.Parser
{
    public class IntellisenseKeyword
    {
        // Fields
        private string name;

        // Methods
        public IntellisenseKeyword(string name)
        {
            this.name = name;
        }

        // Properties
        public string Name
        {
            get
            {
                return this.name;
            }
        }
    }

	public class ParserProxy
	{
		static readonly Type s_Type;
		static readonly MethodInfo s_ParseConditionMethod;
        static readonly MethodInfo s_GetExpressionCompletions;

        static Type _s_IntellisenseKeywordType = null;
        static Type s_IntellisenseKeywordType
        {
            get {
                if(_s_IntellisenseKeywordType == null)
                {
                    _s_IntellisenseKeywordType = Type.GetType("System.Workflow.Activities.Rules.IntellisenseKeyword, System.Workflow.Activities, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", true, true);
                }
                return _s_IntellisenseKeywordType;
            }
        }

        static PropertyInfo _s_IntellisenseKeywordName = null;
        static PropertyInfo s_IntellisenseKeywordName
        {
            get {
                if(_s_IntellisenseKeywordName == null)
                {
                    _s_IntellisenseKeywordName = _s_IntellisenseKeywordType.GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                return _s_IntellisenseKeywordName;
            }
        }

        static MethodInfo _s_ParseStatementList = null;
        static MethodInfo s_ParseStatementList
        {
            get
            {
                if (_s_ParseStatementList == null)
                {
                    _s_ParseStatementList = s_Type.GetMethod("ParseStatementList", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                return _s_ParseStatementList;
            }
        }

		object _parser;

		static ParserProxy()
		{
			s_Type = Type.GetType("System.Workflow.Activities.Rules.Parser, System.Workflow.Activities, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", true, true);
			s_ParseConditionMethod = s_Type.GetMethod("ParseCondition", BindingFlags.Instance | BindingFlags.NonPublic);

            s_GetExpressionCompletions = s_Type.GetMethod("GetExpressionCompletions", BindingFlags.Instance | BindingFlags.NonPublic);            
		}

		public ParserProxy(Type targetType) : this(new RuleValidation(targetType, null))
		{
		}

		public ParserProxy(RuleValidation validation)
		{
			_parser = Activator.CreateInstance(s_Type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { validation }, null);
		}

        public List<RuleAction> ParseStatementList(string statementString)
        {
            try {
                var res = s_ParseStatementList.Invoke(_parser, new object[] { statementString });
                return (List<RuleAction>)res;
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }
                throw;
            }

        }

		public RuleExpressionCondition ParseCondition(string text)
		{
            try
            {
                RuleExpressionCondition cond = (RuleExpressionCondition)s_ParseConditionMethod.Invoke(_parser, new object[] { text });
                return cond;
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }
                throw;
            }
		}

        public ICollection GetExpressionCompletions(string expressionString)
        {
            ICollection res = (ICollection)s_GetExpressionCompletions.Invoke(_parser, new object[] { expressionString });
            return ProcessAutoCompleteList(res);
        }

        private ICollection ProcessAutoCompleteList(ICollection list)
        {
            if (list == null)
                return null;
            List result = new List();
            foreach(object o in list)
            {
                if(o.GetType() == s_IntellisenseKeywordType)
                {
                    var name = (string)s_IntellisenseKeywordName.GetValue(o, null);
                    result.Add(new IntellisenseKeyword(name));
                }
                else {
                    result.Add(o);
                }
            }
            return result;
        }
	}
}
