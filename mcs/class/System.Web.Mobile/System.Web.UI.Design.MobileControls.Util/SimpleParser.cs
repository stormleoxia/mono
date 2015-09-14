using System;
using System.Collections;
using System.Globalization;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Web.RegularExpressions;
namespace System.Web.UI.Design.MobileControls.Util
{
	[Obsolete("The System.Web.Mobile.dll assembly has been deprecated and should no longer be used. For information about how to develop ASP.NET mobile applications, see http://go.microsoft.com/fwlink/?LinkId=157231.")]
	[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
	internal class SimpleParser : BaseParser
	{
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private class TagStack
		{
			private Stack _tagStack;
			internal TagStack() : this(100)
			{
			}
			internal TagStack(int initialCapacity)
			{
				this._tagStack = new Stack(initialCapacity);
			}
			internal void Push(string tagName)
			{
				this._tagStack.Push(tagName.ToLower(CultureInfo.InvariantCulture));
			}
			internal string Pop()
			{
				if (this.IsEmpty())
				{
					return string.Empty;
				}
				return (string)this._tagStack.Pop();
			}
			internal bool IsEmpty()
			{
				return this._tagStack.Count == 0;
			}
		}
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private class ElementTable
		{
			private Hashtable _table;
			internal ElementTable() : this(100)
			{
			}
			internal ElementTable(int initialCapacity)
			{
				this._table = new Hashtable(initialCapacity, StringComparer.OrdinalIgnoreCase);
			}
			internal void Add(string key)
			{
				this._table.Add(key, true);
			}
			internal bool Contains(string key)
			{
				return this._table.Contains(key);
			}
			internal void AddRange(string[] keysCollection)
			{
				for (int i = 0; i < keysCollection.Length; i++)
				{
					string key = keysCollection[i];
					this.Add(key);
				}
			}
		}
		private const int _stackInitialSize = 100;
		private static Regex _unclosedTagRegex;
		private const RegexOptions _options = RegexOptions.Multiline | RegexOptions.Singleline;
		private const string _pattern = "\\G<(?<tagname>[\\w:\\.]+)(\\s+(?<attrname>\\w[-\\w:]*)(\\s*=\\s*\"(?<attrval>[^\"]*)\"|\\s*=\\s*'(?<attrval>[^']*)'|\\s*=\\s*(?<attrval><%#.*?%>)|\\s*=\\s*(?!'|\")(?<attrval>[^\\s=/>]*)(?!'|\")|(?<attrval>\\s*?)))*\\s*(?<empty>)?>";
		private static SimpleParser.ElementTable _endTagOptionalElement;
		private static readonly Regex _tagRegex;
		private static readonly Regex _directiveRegex;
		private static readonly Regex _endtagRegex;
		private static readonly Regex _aspCodeRegex;
		private static readonly Regex _aspExprRegex;
		private static readonly Regex _databindExprRegex;
		private static readonly Regex _commentRegex;
		private static readonly Regex _includeRegex;
		private static readonly Regex _textRegex;
		private static readonly Regex _gtRegex;
		private static readonly Regex _ltRegex;
		private static readonly Regex _serverTagsRegex;
		private static readonly Regex _runatServerRegex;
		private SimpleParser()
		{
		}
		static SimpleParser()
		{
			SimpleParser._unclosedTagRegex = null;
			SimpleParser._endTagOptionalElement = null;
			SimpleParser._tagRegex = new TagRegex();
			SimpleParser._directiveRegex = new DirectiveRegex();
			SimpleParser._endtagRegex = new EndTagRegex();
			SimpleParser._aspCodeRegex = new AspCodeRegex();
			SimpleParser._aspExprRegex = new AspExprRegex();
			SimpleParser._databindExprRegex = new DatabindExprRegex();
			SimpleParser._commentRegex = new CommentRegex();
			SimpleParser._includeRegex = new IncludeRegex();
			SimpleParser._textRegex = new TextRegex();
			SimpleParser._gtRegex = new GTRegex();
			SimpleParser._ltRegex = new LTRegex();
			SimpleParser._serverTagsRegex = new ServerTagsRegex();
			SimpleParser._runatServerRegex = new RunatServerRegex();
			SimpleParser._unclosedTagRegex = new Regex("\\G<(?<tagname>[\\w:\\.]+)(\\s+(?<attrname>\\w[-\\w:]*)(\\s*=\\s*\"(?<attrval>[^\"]*)\"|\\s*=\\s*'(?<attrval>[^']*)'|\\s*=\\s*(?<attrval><%#.*?%>)|\\s*=\\s*(?!'|\")(?<attrval>[^\\s=/>]*)(?!'|\")|(?<attrval>\\s*?)))*\\s*(?<empty>)?>", RegexOptions.Multiline | RegexOptions.Singleline);
			SimpleParser._endTagOptionalElement = new SimpleParser.ElementTable();
			SimpleParser._endTagOptionalElement.AddRange(new string[]
			{
				"area",
				"base",
				"basefront",
				"bgsound",
				"br",
				"col",
				"colgroup",
				"dd",
				"dt",
				"embed",
				"frame",
				"hr",
				"img",
				"input",
				"isindex",
				"li",
				"link",
				"meta",
				"option",
				"p",
				"param",
				"rt"
			});
		}
		internal static bool IsWellFormed(string text)
		{
			int num = 0;
			SimpleParser.TagStack tagStack = new SimpleParser.TagStack();
			while (true)
			{
				Match match;
				if ((match = SimpleParser._textRegex.Match(text, num)).Success)
				{
					num = match.Index + match.Length;
				}
				if (num == text.Length)
				{
					break;
				}
				if ((match = SimpleParser._unclosedTagRegex.Match(text, num)).Success)
				{
					string value = match.Groups["tagname"].Value;
					tagStack.Push(value);
				}
				else
				{
					if (!(match = SimpleParser._tagRegex.Match(text, num)).Success)
					{
						if ((match = SimpleParser._endtagRegex.Match(text, num)).Success)
						{
							string value2 = match.Groups["tagname"].Value;
							bool flag = false;
							while (!tagStack.IsEmpty())
							{
								string text2 = tagStack.Pop();
								if (string.Compare(value2, text2, StringComparison.OrdinalIgnoreCase) == 0)
								{
									flag = true;
									break;
								}
								if (!SimpleParser.IsEndTagOptional(text2))
								{
									return false;
								}
							}
							if (!flag && tagStack.IsEmpty())
							{
								return false;
							}
						}
						else
						{
							if (!(match = SimpleParser._directiveRegex.Match(text, num)).Success && !(match = SimpleParser._includeRegex.Match(text, num)).Success && !(match = SimpleParser._commentRegex.Match(text, num)).Success && !(match = SimpleParser._aspExprRegex.Match(text, num)).Success && !(match = SimpleParser._databindExprRegex.Match(text, num)).Success)
							{
								bool arg_18A_0 = (match = SimpleParser._aspCodeRegex.Match(text, num)).Success;
							}
						}
					}
				}
				if (match == null || !match.Success)
				{
					num++;
				}
				else
				{
					num = match.Index + match.Length;
				}
				if (num == text.Length)
				{
					goto Block_18;
				}
			}
			while (!tagStack.IsEmpty())
			{
				if (!SimpleParser.IsEndTagOptional(tagStack.Pop()))
				{
					return false;
				}
			}
			return true;
			Block_18:
			while (!tagStack.IsEmpty())
			{
				if (!SimpleParser.IsEndTagOptional(tagStack.Pop()))
				{
					return false;
				}
			}
			return true;
		}
		private static bool IsEndTagOptional(string element)
		{
			return SimpleParser._endTagOptionalElement.Contains(element);
		}
	}
}
