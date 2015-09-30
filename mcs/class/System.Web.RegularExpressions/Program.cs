using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace System.Web.RegularExpressions_gen
{
    internal class Program
    {
        private const string AssemblyNamespace = "System.Web.RegularExpressions";

        private static readonly string[][] regexes = new[]
        {
            new[]
            {
                "AspCodeRegex", @"\G<%(?!@)(?<code>.*?)%>"
            },
            new[]
            {
                "AspEncodedExprRegex", @"\G<%:(?<code>.*?)?%>"  
            },            
            new[]
            {
                "AspExprRegex", @"\G<%\s*?=(?<code>.*?)?%>"
            },
            new[]
            {
                "BindExpressionRegex", @"^\s*bind\s*\((?<params>.*)\)\s*\z"
            },
            new[]
            {
                "BindItemExpressionRegex", @"^\s*BindItem\.(?<params>.*)\s*\z"
            },
            new[]
            {
                "BindItemParametersRegex", @"(?<fieldName>([\w\.]+))\s*\z"
            },
            new[]
            {
                "BindParametersRegex",
                "\\s*((\"(?<fieldName>(([\\w\\.]+)|(\\[.+\\])))\")|('(?<fieldName>(([\\w\\.]+)|(\\[.+\\])))'))\\s*(,\\s*((\"(?<formatString>.*)\")|('(?<formatString>.*)'))\\s*)?\\s*\\z"
            },
            new[]
            {
                "BrowserCapsRefRegex", "\\$(?:\\{(?<name>\\w+)\\})"
            },
            new[]
            {
                "CommentRegex", @"\G<%--(([^-]*)-)*?-%>"
            },
            new[]
            {
                "DatabindExprRegex", @"\G<%#(?<encode>:)?(?<code>.*?)?%>"
            },
            new[]
            {
                "DataBindRegex", @"\G\s*<%\s*?#(?<encode>:)?(?<code>.*?)?%>\s*\z"
            },
            new[]
            {
                "DirectiveRegex",
                "\\G<%\\s*@(\\s*(?<attrname>\\w[\\w:]*(?=\\W))(\\s*(?<equal>=)\\s*\"(?<attrval>[^\"]*)\"|\\s*(?<equal>=)\\s*'(?<attrval>[^']*)'|\\s*(?<equal>=)\\s*(?<attrval>[^\\s\"'%>]*)|(?<equal>)(?<attrval>\\s*?)))*\\s*?%>"
            },
            new[]
            {
                "EndTagRegex", @"\G</(?<tagname>[\w:\.]+)\s*>"
            },
            new[]
            {
                "EvalExpressionRegex", "^\\s*eval\\s*\\((?<params>.*)\\)\\s*\\z"
            },
            new[]
            {
                "ExpressionBuilderRegex", "\\G\\s*<%\\s*\\$\\s*(?<code>.*)?%>\\s*\\z"
            },
            new[]
            {
                "FormatStringRegex", "^(([^\"]*(\"\")?)*)$"
            },
            new[]
            {
                "GTRegex", "[^%]>"
            },
            new[]
            {
                "IncludeRegex",
                "\\G<!--\\s*#(?i:include)\\s*(?<pathtype>[\\w]+)\\s*=\\s*[\"']?(?<filename>[^\\\"']*?)[\"']?\\s*-->"
            },
            new[]
            {
                "LTRegex", "<[^%]"
            },
            new[]
            {
                "NonWordRegex", "\\W"
            },
            new[]
            {
                "RunatServerRegex", "runat\\W*server"
            },
            new[]
            {
                "ServerTagsRegex", "<%(?![#$])(([^%]*)%)*?>"
            },
            new[]
            {
                "SimpleDirectiveRegex",
                "<%\\s*@(\\s*(?<attrname>\\w[\\w:]*(?=\\W))(\\s*(?<equal>=)\\s*\"(?<attrval>[^\"]*)\"|\\s*(?<equal>=)\\s*'(?<attrval>[^']*)'|\\s*(?<equal>=)\\s*(?<attrval>[^\\s\"'%>]*)|(?<equal>)(?<attrval>\\s*?)))*\\s*?%>"
            },
            new[]
            {
                "TagRegex",
                "\\G<(?<tagname>[\\w:\\.]+)(\\s+(?<attrname>\\w[-\\w:]*)(\\s*=\\s*\"(?<attrval>[^\"]*)\"|\\s*=\\s*'(?<attrval>[^']*)'|\\s*=\\s*(?<attrval><%#.*?%>)|\\s*=\\s*(?<attrval>[^\\s=\"'/>]*)|(?<attrval>\\s*?)))*\\s*(?<empty>/)?>"
            },
            new[]
            {
                "TextRegex", "\\G[^<]+"
            },
            new[]
            {
                "TagRegex35", "\\G<(?<tagname>[\\w:\\.]+)(\\s+(?<attrname>\\w[-\\w:]*)(\\s*=\\s*\"(?<attrval>[^\"]*)\"|\\s*=\\s*'(?<attrval>[^']*)'|\\s*=\\s*(?<attrval><%#.*?%>)|\\s*=\\s*(?<attrval>[^\\s=/>]*)|(?<attrval>\\s*?)))*\\s*(?<empty>/)?>"
  
            },  
            new[]
            {
                "WebResourceRegex", "<%\\s*=\\s*WebResource\\(\"(?<resourceName>[^\"]*)\"\\)\\s*%>"
            }
        };

        private static void Main(string[] args)
        {
            try
            {
                var compilationInfos = new List<RegexCompilationInfo>(10);
                foreach (var record in regexes)
                {
                    compilationInfos.Add(new RegexCompilationInfo(record[1],
                        RegexOptions.Multiline | RegexOptions.Singleline,
                        record[0],
                        AssemblyNamespace,
                        true));
                }
                string firstArgument = null;
                if (args.Length > 0)
                {
                    firstArgument = args[0];
                }
                else
                {
                    Console.WriteLine("No KeyPair file provided: no signing possible.");
                }
                StrongNameKeyPair keyPair = null;
                if (!string.IsNullOrEmpty(firstArgument))
                {
                    if (File.Exists(firstArgument))
                    {
                        Console.WriteLine("Using {0} as KeyPair file for {1}", firstArgument, AssemblyNamespace);
                        using (var stream = File.Open(firstArgument, FileMode.Open))
                        {
                            keyPair = new StrongNameKeyPair(stream);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(firstArgument + " doesn't exists.");
                    }
                }
                AssemblyName assemblyName = new AssemblyName(AssemblyNamespace + ", " +
                                                             "Version=" +
                                                             Consts.MonoVersion + ", " +
                                                             "Culture=" +
                                                             "neutral" + ", " +
                                                             "PublicKeyToken=" +
                                                             "null");
                if (keyPair != null)
                {
                    assemblyName.KeyPair = keyPair;
                    assemblyName.HashAlgorithm = AssemblyHashAlgorithm.SHA512;                    
                    assemblyName.SetPublicKeyToken(assemblyName.GetPublicKeyToken());
                }
                Regex.CompileToAssembly(compilationInfos.ToArray(), assemblyName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }
    }
}
