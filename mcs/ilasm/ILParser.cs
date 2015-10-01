// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 2 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
//
// Mono::ILASM::ILParser
// 
// (C) Sergey Chaban (serge@wildwestsoftware.com)
// (C) 2003 Jackson Harper, All rights reserved
//

using PEAPI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

using MIPermission = Mono.ILASM.Permission;
using MIPermissionSet = Mono.ILASM.PermissionSet;

namespace Mono.ILASM {

	public class ILParser {

		private CodeGen codegen;

		private bool is_value_class;
		private bool is_enum_class;
                private bool pinvoke_info;
                private string pinvoke_mod;
                private string pinvoke_meth;
                private PEAPI.PInvokeAttr pinvoke_attr;
                private ILTokenizer tokenizer;
		static int yacc_verbose_flag;
		KeyValuePair<string, TypeAttr> current_extern;

                class NameValuePair {
                        public string Name;
                        public object Value;

                        public NameValuePair (string name, object value)
                        {
                                this.Name = name;
                                this.Value = value;
                        }
                }

                class PermPair {
                        public PEAPI.SecurityAction sec_action;
                        public object perm;

                        public PermPair (PEAPI.SecurityAction sec_action, object perm)
                        {
                                this.sec_action = sec_action;
                                this.perm = perm;
                        }
                }

                public bool CheckSecurityActionValidity (System.Security.Permissions.SecurityAction action, bool for_assembly)
                {
                        if ((action == System.Security.Permissions.SecurityAction.RequestMinimum || 
                                action == System.Security.Permissions.SecurityAction.RequestOptional || 
                                action == System.Security.Permissions.SecurityAction.RequestRefuse) && !for_assembly) {
                                Report.Warning (String.Format ("System.Security.Permissions.SecurityAction '{0}' is not valid for this declaration", action));
                                return false;
                        }

                        return true;
                }

		public void AddSecDecl (object perm, bool for_assembly)
		{
			PermPair pp = perm as PermPair;

			if (pp == null) {
				MIPermissionSet ps_20 = (MIPermissionSet) perm;
				codegen.AddPermission (ps_20.SecurityAction, ps_20);
				return;
			}

			if (!CheckSecurityActionValidity ((System.Security.Permissions.SecurityAction) pp.sec_action, for_assembly))
				Report.Error (String.Format ("Invalid security action : {0}", pp.sec_action));

			codegen.AddPermission (pp.sec_action, pp.perm);
		}

                public object ClassRefToObject (object class_ref, object val)
                {
                        ExternTypeRef etr = class_ref as ExternTypeRef;
                        if (etr == null)
                                /* FIXME: report error? can be PrimitiveTypeRef or TypeRef */
                                return null;
                                
                        System.Type t = etr.GetReflectedType ();
                        return (t.IsEnum ? Enum.Parse (t, String.Format ("{0}", val)) : val);
                }

		/* Converts a type_spec to a corresponding PermPair */
                PermPair TypeSpecToPermPair (object action, object type_spec, ArrayList pairs)
                {
                        ExternTypeRef etr = type_spec as ExternTypeRef;
                        if (etr == null)
                                /* FIXME: could be PrimitiveTypeRef or TypeRef 
                                          Report what error? */
                                return null;

                        System.Type t = etr.GetReflectedType ();
                        object obj = Activator.CreateInstance (t, 
                                                new object [] {(System.Security.Permissions.SecurityAction) (short) action});

                        if (pairs != null)
                                foreach (NameValuePair pair in pairs) {
                                        PropertyInfo pi = t.GetProperty (pair.Name);
                                        pi.SetValue (obj, pair.Value, null);
                                }

                        IPermission iper = (IPermission) t.GetMethod ("CreatePermission").Invoke (obj, null);
                        return new PermPair ((PEAPI.SecurityAction) action, iper);
                }

		public ILParser (CodeGen codegen, ILTokenizer tokenizer)
                {
			this.codegen = codegen;
                        this.tokenizer = tokenizer;
		}

		public CodeGen CodeGen {
			get { return codegen; }
		}

                private BaseTypeRef GetTypeRef (BaseTypeRef b)
                {
                        //FIXME: Caching required.. 
                        return b.Clone ();
                }

#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }
#pragma warning disable 649
  /* An EOF token */
  public int eof_token;
#pragma warning restore 649
  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected const int yyFinal = 1;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static readonly string [] yyRule = {
    "$accept : il_file",
    "il_file : decls",
    "decls :",
    "decls : decls decl",
    "decl : class_all",
    "decl : namespace_all",
    "decl : method_all",
    "decl : field_decl",
    "decl : data_decl",
    "decl : vtfixup_decl",
    "decl : file_decl",
    "decl : assembly_all",
    "decl : assemblyref_all",
    "decl : exptype_all",
    "decl : manifestres_all",
    "decl : module_head",
    "decl : sec_decl",
    "decl : customattr_decl",
    "decl : D_SUBSYSTEM int32",
    "decl : D_CORFLAGS int32",
    "decl : D_FILE K_ALIGNMENT int32",
    "decl : D_IMAGEBASE int64",
    "decl : D_STACKRESERVE int64",
    "decl : extsource_spec",
    "decl : language_decl",
    "extsource_spec : D_LINE int32 SQSTRING",
    "extsource_spec : D_LINE int32",
    "extsource_spec : D_LINE int32 COLON int32 SQSTRING",
    "extsource_spec : D_LINE int32 COLON int32",
    "language_decl : D_LANGUAGE SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING COMMA SQSTRING",
    "vtfixup_decl : D_VTFIXUP OPEN_BRACKET int32 CLOSE_BRACKET vtfixup_attr K_AT id",
    "vtfixup_attr :",
    "vtfixup_attr : vtfixup_attr K_INT32",
    "vtfixup_attr : vtfixup_attr K_INT64",
    "vtfixup_attr : vtfixup_attr K_FROMUNMANAGED",
    "vtfixup_attr : vtfixup_attr K_CALLMOSTDERIVED",
    "namespace_all : namespace_head OPEN_BRACE decls CLOSE_BRACE",
    "namespace_head : D_NAMESPACE comp_name",
    "class_all : class_head OPEN_BRACE class_decls CLOSE_BRACE",
    "class_head : D_CLASS class_attr comp_name formal_typars_clause extends_clause impl_clause",
    "class_attrs : class_attrs class_attr",
    "class_attr :",
    "class_attr : class_attr K_PUBLIC",
    "class_attr : class_attr K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PUBLIC",
    "class_attr : class_attr K_NESTED K_FAMILY",
    "class_attr : class_attr K_NESTED K_ASSEMBLY",
    "class_attr : class_attr K_NESTED K_FAMANDASSEM",
    "class_attr : class_attr K_NESTED K_FAMORASSEM",
    "class_attr : class_attr K_VALUE",
    "class_attr : class_attr K_ENUM",
    "class_attr : class_attr K_INTERFACE",
    "class_attr : class_attr K_SEALED",
    "class_attr : class_attr K_ABSTRACT",
    "class_attr : class_attr K_AUTO",
    "class_attr : class_attr K_SEQUENTIAL",
    "class_attr : class_attr K_EXPLICIT",
    "class_attr : class_attr K_ANSI",
    "class_attr : class_attr K_UNICODE",
    "class_attr : class_attr K_AUTOCHAR",
    "class_attr : class_attr K_IMPORT",
    "class_attr : class_attr K_SERIALIZABLE",
    "class_attr : class_attr K_BEFOREFIELDINIT",
    "class_attr : class_attr K_SPECIALNAME",
    "class_attr : class_attr K_RTSPECIALNAME",
    "extends_clause :",
    "extends_clause : K_EXTENDS generic_class_ref",
    "impl_clause :",
    "impl_clause : impl_class_refs",
    "impl_class_refs : K_IMPLEMENTS generic_class_ref",
    "impl_class_refs : impl_class_refs COMMA generic_class_ref",
    "formal_typars_clause :",
    "formal_typars_clause : OPEN_ANGLE_BRACKET formal_typars CLOSE_ANGLE_BRACKET",
    "typars_clause :",
    "typars_clause : OPEN_ANGLE_BRACKET typars CLOSE_ANGLE_BRACKET",
    "typars : type",
    "typars : typars COMMA type",
    "constraints_clause :",
    "constraints_clause : OPEN_PARENS constraints CLOSE_PARENS",
    "constraints : generic_class_ref",
    "constraints : constraints COMMA generic_class_ref",
    "generic_class_ref : class_ref",
    "generic_class_ref : K_CLASS class_ref typars_clause",
    "generic_class_ref : BANG int32",
    "generic_class_ref : BANG BANG int32",
    "generic_class_ref : BANG id",
    "generic_class_ref : BANG BANG id",
    "formal_typars : formal_typar_attr constraints_clause formal_typar",
    "formal_typars : formal_typars COMMA formal_typar_attr constraints_clause formal_typar",
    "formal_typar_attr :",
    "formal_typar_attr : formal_typar_attr PLUS",
    "formal_typar_attr : formal_typar_attr DASH",
    "formal_typar_attr : formal_typar_attr D_CTOR",
    "formal_typar_attr : formal_typar_attr K_VALUETYPE",
    "formal_typar_attr : formal_typar_attr K_CLASS",
    "formal_typar : id",
    "param_type_decl : D_PARAM K_TYPE id",
    "param_type_decl : D_PARAM K_TYPE OPEN_BRACKET int32 CLOSE_BRACKET",
    "class_refs : class_ref",
    "class_refs : class_refs COMMA class_ref",
    "slashed_name : comp_name",
    "slashed_name : slashed_name SLASH comp_name",
    "class_ref : OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : slashed_name",
    "class_decls :",
    "class_decls : class_decls class_decl",
    "class_decl : method_all",
    "class_decl : class_all",
    "class_decl : event_all",
    "class_decl : prop_all",
    "class_decl : field_decl",
    "class_decl : data_decl",
    "class_decl : sec_decl",
    "class_decl : extsource_spec",
    "class_decl : customattr_decl",
    "class_decl : param_type_decl",
    "class_decl : D_SIZE int32",
    "class_decl : D_PACK int32",
    "$$1 :",
    "class_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name K_WITH call_conv type type_spec DOUBLE_COLON method_name type_list $$1 OPEN_PARENS sig_args CLOSE_PARENS",
    "class_decl : language_decl",
    "type : generic_class_ref",
    "type : K_OBJECT",
    "type : K_VALUE K_CLASS class_ref",
    "type : K_VALUETYPE OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name typars_clause",
    "type : K_VALUETYPE slashed_name typars_clause",
    "type : type OPEN_BRACKET CLOSE_BRACKET",
    "type : type OPEN_BRACKET bounds CLOSE_BRACKET",
    "type : type AMPERSAND",
    "type : type STAR",
    "type : type K_PINNED",
    "type : type K_MODREQ OPEN_PARENS class_ref CLOSE_PARENS",
    "type : type K_MODOPT OPEN_PARENS class_ref CLOSE_PARENS",
    "type : K_METHOD call_conv type STAR OPEN_PARENS sig_args CLOSE_PARENS",
    "type : primitive_type",
    "primitive_type : K_INT8",
    "primitive_type : K_INT16",
    "primitive_type : K_INT32",
    "primitive_type : K_INT64",
    "primitive_type : K_FLOAT32",
    "primitive_type : K_FLOAT64",
    "primitive_type : K_UNSIGNED K_INT8",
    "primitive_type : K_UINT8",
    "primitive_type : K_UNSIGNED K_INT16",
    "primitive_type : K_UINT16",
    "primitive_type : K_UNSIGNED K_INT32",
    "primitive_type : K_UINT32",
    "primitive_type : K_UNSIGNED K_INT64",
    "primitive_type : K_UINT64",
    "primitive_type : K_NATIVE K_INT",
    "primitive_type : K_NATIVE K_UNSIGNED K_INT",
    "primitive_type : K_NATIVE K_UINT",
    "primitive_type : K_TYPEDREF",
    "primitive_type : K_CHAR",
    "primitive_type : K_WCHAR",
    "primitive_type : K_VOID",
    "primitive_type : K_BOOL",
    "primitive_type : K_STRING",
    "bounds : bound",
    "bounds : bounds COMMA bound",
    "bound :",
    "bound : ELLIPSIS",
    "bound : int32",
    "bound : int32 ELLIPSIS int32",
    "bound : int32 ELLIPSIS",
    "call_conv : K_INSTANCE call_conv",
    "call_conv : K_EXPLICIT call_conv",
    "call_conv : call_kind",
    "call_kind :",
    "call_kind : K_DEFAULT",
    "call_kind : K_VARARG",
    "call_kind : K_UNMANAGED K_CDECL",
    "call_kind : K_UNMANAGED K_STDCALL",
    "call_kind : K_UNMANAGED K_THISCALL",
    "call_kind : K_UNMANAGED K_FASTCALL",
    "native_type :",
    "native_type : K_CUSTOM OPEN_PARENS comp_qstring COMMA comp_qstring CLOSE_PARENS",
    "native_type : K_FIXED K_SYSSTRING OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_FIXED K_ARRAY OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_VARIANT",
    "native_type : K_CURRENCY",
    "native_type : K_SYSCHAR",
    "native_type : K_VOID",
    "native_type : K_BOOL",
    "native_type : K_INT8",
    "native_type : K_INT16",
    "native_type : K_INT32",
    "native_type : K_INT64",
    "native_type : K_FLOAT32",
    "native_type : K_FLOAT64",
    "native_type : K_ERROR",
    "native_type : K_UNSIGNED K_INT8",
    "native_type : K_UINT8",
    "native_type : K_UNSIGNED K_INT16",
    "native_type : K_UINT16",
    "native_type : K_UNSIGNED K_INT32",
    "native_type : K_UINT32",
    "native_type : K_UNSIGNED K_INT64",
    "native_type : K_UINT64",
    "native_type : native_type STAR",
    "native_type : native_type OPEN_BRACKET CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 PLUS int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET PLUS int32 CLOSE_BRACKET",
    "native_type : K_DECIMAL",
    "native_type : K_DATE",
    "native_type : K_BSTR",
    "native_type : K_LPSTR",
    "native_type : K_LPWSTR",
    "native_type : K_LPTSTR",
    "native_type : K_OBJECTREF",
    "native_type : K_IUNKNOWN",
    "native_type : K_IDISPATCH",
    "native_type : K_STRUCT",
    "native_type : K_INTERFACE",
    "native_type : K_SAFEARRAY variant_type",
    "native_type : K_SAFEARRAY variant_type COMMA comp_qstring",
    "native_type : K_INT",
    "native_type : K_UNSIGNED K_INT",
    "native_type : K_NESTED K_STRUCT",
    "native_type : K_BYVALSTR",
    "native_type : K_ANSI K_BSTR",
    "native_type : K_TBSTR",
    "native_type : K_VARIANT K_BOOL",
    "native_type : K_METHOD",
    "native_type : K_AS K_ANY",
    "native_type : K_LPSTRUCT",
    "variant_type :",
    "variant_type : K_NULL",
    "variant_type : K_VARIANT",
    "variant_type : K_CURRENCY",
    "variant_type : K_VOID",
    "variant_type : K_BOOL",
    "variant_type : K_INT8",
    "variant_type : K_INT16",
    "variant_type : K_INT32",
    "variant_type : K_INT64",
    "variant_type : K_FLOAT32",
    "variant_type : K_FLOAT64",
    "variant_type : K_UNSIGNED K_INT8",
    "variant_type : K_UNSIGNED K_INT16",
    "variant_type : K_UNSIGNED K_INT32",
    "variant_type : K_UNSIGNED K_INT64",
    "variant_type : STAR",
    "variant_type : variant_type OPEN_BRACKET CLOSE_BRACKET",
    "variant_type : variant_type K_VECTOR",
    "variant_type : variant_type AMPERSAND",
    "variant_type : K_DECIMAL",
    "variant_type : K_DATE",
    "variant_type : K_BSTR",
    "variant_type : K_LPSTR",
    "variant_type : K_LPWSTR",
    "variant_type : K_IUNKNOWN",
    "variant_type : K_IDISPATCH",
    "variant_type : K_SAFEARRAY",
    "variant_type : K_INT",
    "variant_type : K_UNSIGNED K_INT",
    "variant_type : K_ERROR",
    "variant_type : K_HRESULT",
    "variant_type : K_CARRAY",
    "variant_type : K_USERDEFINED",
    "variant_type : K_RECORD",
    "variant_type : K_FILETIME",
    "variant_type : K_BLOB",
    "variant_type : K_STREAM",
    "variant_type : K_STORAGE",
    "variant_type : K_STREAMED_OBJECT",
    "variant_type : K_STORED_OBJECT",
    "variant_type : K_BLOB_OBJECT",
    "variant_type : K_CF",
    "variant_type : K_CLSID",
    "field_decl : D_FIELD repeat_opt field_attr type id at_opt init_opt",
    "repeat_opt :",
    "repeat_opt : OPEN_BRACKET int32 CLOSE_BRACKET",
    "field_attr :",
    "field_attr : field_attr K_PUBLIC",
    "field_attr : field_attr K_PRIVATE",
    "field_attr : field_attr K_FAMILY",
    "field_attr : field_attr K_ASSEMBLY",
    "field_attr : field_attr K_FAMANDASSEM",
    "field_attr : field_attr K_FAMORASSEM",
    "field_attr : field_attr K_PRIVATESCOPE",
    "field_attr : field_attr K_STATIC",
    "field_attr : field_attr K_INITONLY",
    "field_attr : field_attr K_RTSPECIALNAME",
    "field_attr : field_attr K_SPECIALNAME",
    "field_attr : field_attr K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "field_attr : field_attr K_LITERAL",
    "field_attr : field_attr K_NOTSERIALIZED",
    "at_opt :",
    "at_opt : K_AT id",
    "init_opt :",
    "init_opt : ASSIGN field_init",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_CHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_WCHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_BOOL OPEN_PARENS truefalse CLOSE_PARENS",
    "field_init : field_init_primitive",
    "field_init : K_BYTEARRAY bytes_list",
    "field_init : comp_qstring",
    "field_init : K_NULLREF",
    "data_decl : data_head data_body",
    "data_head : D_DATA tls id ASSIGN",
    "data_head : D_DATA tls",
    "tls :",
    "tls : K_TLS",
    "data_body : OPEN_BRACE dataitem_list CLOSE_BRACE",
    "data_body : dataitem",
    "dataitem_list : dataitem",
    "dataitem_list : dataitem_list COMMA dataitem",
    "dataitem : K_CHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : K_WCHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : AMPERSAND OPEN_PARENS id CLOSE_PARENS",
    "dataitem : K_BYTEARRAY ASSIGN bytes_list",
    "dataitem : K_BYTEARRAY bytes_list",
    "dataitem : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT64 OPEN_PARENS int64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT32 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT16 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT8 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT32 repeat_opt",
    "dataitem : K_FLOAT64 repeat_opt",
    "dataitem : K_INT64 repeat_opt",
    "dataitem : K_INT32 repeat_opt",
    "dataitem : K_INT16 repeat_opt",
    "dataitem : K_INT8 repeat_opt",
    "method_all : method_head OPEN_BRACE method_decls CLOSE_BRACE",
    "method_head : D_METHOD meth_attr call_conv param_attr type method_name formal_typars_clause OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "method_head : D_METHOD meth_attr call_conv param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS method_name OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "meth_attr :",
    "meth_attr : meth_attr K_STATIC",
    "meth_attr : meth_attr K_PUBLIC",
    "meth_attr : meth_attr K_PRIVATE",
    "meth_attr : meth_attr K_FAMILY",
    "meth_attr : meth_attr K_ASSEMBLY",
    "meth_attr : meth_attr K_FAMANDASSEM",
    "meth_attr : meth_attr K_FAMORASSEM",
    "meth_attr : meth_attr K_PRIVATESCOPE",
    "meth_attr : meth_attr K_FINAL",
    "meth_attr : meth_attr K_VIRTUAL",
    "meth_attr : meth_attr K_ABSTRACT",
    "meth_attr : meth_attr K_HIDEBYSIG",
    "meth_attr : meth_attr K_NEWSLOT",
    "meth_attr : meth_attr K_REQSECOBJ",
    "meth_attr : meth_attr K_SPECIALNAME",
    "meth_attr : meth_attr K_RTSPECIALNAME",
    "meth_attr : meth_attr K_STRICT",
    "meth_attr : meth_attr K_COMPILERCONTROLLED",
    "meth_attr : meth_attr K_UNMANAGEDEXP",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring K_AS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS pinv_attr CLOSE_PARENS",
    "pinv_attr :",
    "pinv_attr : pinv_attr K_NOMANGLE",
    "pinv_attr : pinv_attr K_ANSI",
    "pinv_attr : pinv_attr K_UNICODE",
    "pinv_attr : pinv_attr K_AUTOCHAR",
    "pinv_attr : pinv_attr K_LASTERR",
    "pinv_attr : pinv_attr K_WINAPI",
    "pinv_attr : pinv_attr K_CDECL",
    "pinv_attr : pinv_attr K_STDCALL",
    "pinv_attr : pinv_attr K_THISCALL",
    "pinv_attr : pinv_attr K_FASTCALL",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_ON",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_OFF",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_ON",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_OFF",
    "method_name : D_CTOR",
    "method_name : D_CCTOR",
    "method_name : comp_name",
    "param_attr :",
    "param_attr : param_attr OPEN_BRACKET K_IN CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OUT CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OPT CLOSE_BRACKET",
    "impl_attr :",
    "impl_attr : impl_attr K_NATIVE",
    "impl_attr : impl_attr K_CIL",
    "impl_attr : impl_attr K_IL",
    "impl_attr : impl_attr K_OPTIL",
    "impl_attr : impl_attr K_MANAGED",
    "impl_attr : impl_attr K_UNMANAGED",
    "impl_attr : impl_attr K_FORWARDREF",
    "impl_attr : impl_attr K_PRESERVESIG",
    "impl_attr : impl_attr K_RUNTIME",
    "impl_attr : impl_attr K_INTERNALCALL",
    "impl_attr : impl_attr K_SYNCHRONIZED",
    "impl_attr : impl_attr K_NOINLINING",
    "sig_args :",
    "sig_args : sig_arg_list",
    "sig_arg_list : sig_arg",
    "sig_arg_list : sig_arg_list COMMA sig_arg",
    "sig_arg : param_attr type",
    "sig_arg : param_attr type id",
    "sig_arg : ELLIPSIS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS id",
    "type_list :",
    "type_list : ELLIPSIS",
    "type_list : type_list COMMA ELLIPSIS",
    "type_list : param_attr type opt_id",
    "type_list : type_list COMMA param_attr type opt_id",
    "opt_id :",
    "opt_id : id",
    "method_decls :",
    "method_decls : method_decls method_decl",
    "method_decl : D_EMITBYTE int32",
    "method_decl : D_MAXSTACK int32",
    "method_decl : D_LOCALS OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_LOCALS K_INIT OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_ENTRYPOINT",
    "method_decl : D_ZEROINIT",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET K_AS id",
    "method_decl : D_VTENTRY int32 COLON int32",
    "method_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name",
    "method_decl : D_OVERRIDE K_METHOD method_ref",
    "method_decl : D_OVERRIDE K_METHOD call_conv type type_spec DOUBLE_COLON method_name OPEN_ANGLE_BRACKET OPEN_BRACKET int32 CLOSE_BRACKET CLOSE_ANGLE_BRACKET OPEN_PARENS type_list CLOSE_PARENS",
    "method_decl : scope_block",
    "method_decl : D_PARAM OPEN_BRACKET int32 CLOSE_BRACKET init_opt",
    "method_decl : param_type_decl",
    "method_decl : id COLON",
    "method_decl : seh_block",
    "method_decl : instr",
    "method_decl : sec_decl",
    "method_decl : extsource_spec",
    "method_decl : language_decl",
    "method_decl : customattr_decl",
    "method_decl : data_decl",
    "local_list :",
    "local_list : local",
    "local_list : local_list COMMA local",
    "local : type",
    "local : type id",
    "local : slot_num type",
    "local : slot_num type id",
    "slot_num : OPEN_BRACKET int32 CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET slashed_name CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET",
    "type_spec : type",
    "scope_block : scope_block_begin method_decls CLOSE_BRACE",
    "scope_block_begin : OPEN_BRACE",
    "seh_block : try_block seh_clauses",
    "try_block : D_TRY scope_block",
    "try_block : D_TRY id K_TO id",
    "try_block : D_TRY int32 K_TO int32",
    "seh_clauses : seh_clause",
    "seh_clauses : seh_clauses seh_clause",
    "seh_clause : K_CATCH type handler_block",
    "seh_clause : K_FINALLY handler_block",
    "seh_clause : K_FAULT handler_block",
    "seh_clause : filter_clause handler_block",
    "filter_clause : K_FILTER scope_block",
    "filter_clause : K_FILTER id",
    "filter_clause : K_FILTER int32",
    "handler_block : scope_block",
    "handler_block : K_HANDLER id K_TO id",
    "handler_block : K_HANDLER int32 K_TO int32",
    "instr : INSTR_NONE",
    "instr : INSTR_LOCAL int32",
    "instr : INSTR_LOCAL id",
    "instr : INSTR_PARAM int32",
    "instr : INSTR_PARAM id",
    "instr : INSTR_I int32",
    "instr : INSTR_I id",
    "instr : INSTR_I8 int64",
    "instr : INSTR_R float64",
    "instr : INSTR_R int64",
    "instr : INSTR_R bytes_list",
    "instr : INSTR_BRTARGET int32",
    "instr : INSTR_BRTARGET id",
    "instr : INSTR_METHOD method_ref",
    "instr : INSTR_FIELD type type_spec DOUBLE_COLON id",
    "instr : INSTR_FIELD type id",
    "instr : INSTR_TYPE type_spec",
    "instr : INSTR_STRING comp_qstring",
    "instr : INSTR_STRING K_BYTEARRAY ASSIGN bytes_list",
    "instr : INSTR_STRING K_BYTEARRAY bytes_list",
    "instr : INSTR_SIG call_conv type OPEN_PARENS type_list CLOSE_PARENS",
    "instr : INSTR_TOK owner_type",
    "instr : INSTR_SWITCH OPEN_PARENS labels CLOSE_PARENS",
    "method_ref : call_conv type method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "method_ref : call_conv type type_spec DOUBLE_COLON method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "labels :",
    "labels : id",
    "labels : int32",
    "labels : labels COMMA id",
    "labels : labels COMMA int32",
    "owner_type : type_spec",
    "owner_type : member_ref",
    "member_ref : K_METHOD method_ref",
    "member_ref : K_FIELD type type_spec DOUBLE_COLON id",
    "member_ref : K_FIELD type id",
    "event_all : event_head OPEN_BRACE event_decls CLOSE_BRACE",
    "event_head : D_EVENT event_attr type_spec comp_name",
    "event_head : D_EVENT event_attr id",
    "event_attr :",
    "event_attr : event_attr K_RTSPECIALNAME",
    "event_attr : event_attr K_SPECIALNAME",
    "event_decls :",
    "event_decls : event_decls event_decl",
    "event_decl : D_ADDON method_ref",
    "event_decl : D_REMOVEON method_ref",
    "event_decl : D_FIRE method_ref",
    "event_decl : D_OTHER method_ref",
    "event_decl : customattr_decl",
    "event_decl : extsource_spec",
    "event_decl : language_decl",
    "prop_all : prop_head OPEN_BRACE prop_decls CLOSE_BRACE",
    "prop_head : D_PROPERTY prop_attr type comp_name OPEN_PARENS type_list CLOSE_PARENS init_opt",
    "prop_attr :",
    "prop_attr : prop_attr K_RTSPECIALNAME",
    "prop_attr : prop_attr K_SPECIALNAME",
    "prop_attr : prop_attr K_INSTANCE",
    "prop_decls :",
    "prop_decls : prop_decls prop_decl",
    "prop_decl : D_SET method_ref",
    "prop_decl : D_GET method_ref",
    "prop_decl : D_OTHER method_ref",
    "prop_decl : customattr_decl",
    "prop_decl : extsource_spec",
    "prop_decl : language_decl",
    "customattr_decl : D_CUSTOM custom_type",
    "customattr_decl : D_CUSTOM custom_type ASSIGN comp_qstring",
    "customattr_decl : D_CUSTOM custom_type ASSIGN bytes_list",
    "customattr_decl : D_CUSTOM OPEN_PARENS owner_type CLOSE_PARENS custom_type",
    "customattr_decl : D_CUSTOM OPEN_PARENS owner_type CLOSE_PARENS custom_type ASSIGN comp_qstring",
    "customattr_decl : D_CUSTOM OPEN_PARENS owner_type CLOSE_PARENS custom_type ASSIGN bytes_list",
    "custom_type : call_conv type type_spec DOUBLE_COLON method_name OPEN_PARENS type_list CLOSE_PARENS",
    "custom_type : call_conv type method_name OPEN_PARENS type_list CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec OPEN_PARENS nameval_pairs CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN bytes_list",
    "sec_decl : D_PERMISSIONSET sec_action comp_qstring",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN OPEN_BRACE permissions CLOSE_BRACE",
    "permissions : permission",
    "permissions : permissions COMMA permission",
    "permission : class_ref ASSIGN OPEN_BRACE permission_members CLOSE_BRACE",
    "permission_members : permission_member",
    "permission_members : permission_members permission_member",
    "permission_member : prop_or_field primitive_type perm_mbr_nameval_pair",
    "permission_member : prop_or_field K_ENUM class_ref perm_mbr_nameval_pair",
    "perm_mbr_nameval_pair : SQSTRING ASSIGN field_init_primitive",
    "perm_mbr_nameval_pair : SQSTRING ASSIGN K_BYTEARRAY bytes_list",
    "perm_mbr_nameval_pair : SQSTRING ASSIGN K_STRING OPEN_PARENS SQSTRING CLOSE_PARENS",
    "prop_or_field : K_PROPERTY",
    "prop_or_field : K_FIELD",
    "nameval_pairs : nameval_pair",
    "nameval_pairs : nameval_pairs COMMA nameval_pair",
    "nameval_pair : comp_qstring ASSIGN cavalue",
    "cavalue : truefalse",
    "cavalue : int32",
    "cavalue : int32 OPEN_PARENS int32 CLOSE_PARENS",
    "cavalue : comp_qstring",
    "cavalue : class_ref OPEN_PARENS K_INT8 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT16 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT32 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS int32 CLOSE_PARENS",
    "sec_action : K_REQUEST",
    "sec_action : K_DEMAND",
    "sec_action : K_ASSERT",
    "sec_action : K_DENY",
    "sec_action : K_PERMITONLY",
    "sec_action : K_LINKCHECK",
    "sec_action : K_INHERITCHECK",
    "sec_action : K_REQMIN",
    "sec_action : K_REQOPT",
    "sec_action : K_REQREFUSE",
    "sec_action : K_PREJITGRANT",
    "sec_action : K_PREJITDENY",
    "sec_action : K_NONCASDEMAND",
    "sec_action : K_NONCASLINKDEMAND",
    "sec_action : K_NONCASINHERITANCE",
    "module_head : D_MODULE",
    "module_head : D_MODULE comp_name",
    "module_head : D_MODULE K_EXTERN comp_name",
    "file_decl : D_FILE file_attr comp_name file_entry D_HASH ASSIGN bytes_list file_entry",
    "file_decl : D_FILE file_attr comp_name file_entry",
    "file_attr :",
    "file_attr : file_attr K_NOMETADATA",
    "file_entry :",
    "file_entry : D_ENTRYPOINT",
    "assembly_all : assembly_head OPEN_BRACE assembly_decls CLOSE_BRACE",
    "assembly_head : D_ASSEMBLY asm_attr slashed_name",
    "asm_attr :",
    "asm_attr : asm_attr K_RETARGETABLE",
    "assembly_decls :",
    "assembly_decls : assembly_decls assembly_decl",
    "assembly_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assembly_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assembly_decl : D_LOCALE comp_qstring",
    "assembly_decl : D_LOCALE ASSIGN bytes_list",
    "assembly_decl : D_HASH K_ALGORITHM int32",
    "assembly_decl : customattr_decl",
    "assembly_decl : sec_decl",
    "asm_or_ref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "asm_or_ref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "asm_or_ref_decl : D_LOCALE comp_qstring",
    "asm_or_ref_decl : D_LOCALE ASSIGN bytes_list",
    "asm_or_ref_decl : customattr_decl",
    "assemblyref_all : assemblyref_head OPEN_BRACE assemblyref_decls CLOSE_BRACE",
    "assemblyref_head : D_ASSEMBLY K_EXTERN asm_attr slashed_name",
    "assemblyref_head : D_ASSEMBLY K_EXTERN asm_attr slashed_name K_AS slashed_name",
    "assemblyref_decls :",
    "assemblyref_decls : assemblyref_decls assemblyref_decl",
    "assemblyref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assemblyref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assemblyref_decl : D_PUBLICKEYTOKEN ASSIGN bytes_list",
    "assemblyref_decl : D_LOCALE comp_qstring",
    "assemblyref_decl : D_LOCALE ASSIGN bytes_list",
    "assemblyref_decl : D_HASH ASSIGN bytes_list",
    "assemblyref_decl : customattr_decl",
    "exptype_all : exptype_head OPEN_BRACE exptype_decls CLOSE_BRACE",
    "exptype_head : D_CLASS K_EXTERN expt_attr comp_name",
    "expt_attr :",
    "expt_attr : expt_attr K_PRIVATE",
    "expt_attr : expt_attr K_PUBLIC",
    "expt_attr : expt_attr K_NESTED K_PUBLIC",
    "expt_attr : expt_attr K_NESTED K_PRIVATE",
    "expt_attr : expt_attr K_NESTED K_FAMILY",
    "expt_attr : expt_attr K_NESTED K_ASSEMBLY",
    "expt_attr : expt_attr K_NESTED K_FAMANDASSEM",
    "expt_attr : expt_attr K_NESTED K_FAMORASSEM",
    "expt_attr : K_FORWARDER",
    "exptype_decls :",
    "exptype_decls : exptype_decls exptype_decl",
    "exptype_decl : D_FILE comp_name",
    "exptype_decl : D_CLASS K_EXTERN comp_name",
    "exptype_decl : customattr_decl",
    "exptype_decl : D_ASSEMBLY K_EXTERN comp_name",
    "manifestres_all : manifestres_head OPEN_BRACE manifestres_decls CLOSE_BRACE",
    "manifestres_head : D_MRESOURCE manres_attr comp_name",
    "manres_attr :",
    "manres_attr : manres_attr K_PUBLIC",
    "manres_attr : manres_attr K_PRIVATE",
    "manifestres_decls :",
    "manifestres_decls : manifestres_decls manifestres_decl",
    "manifestres_decl : D_FILE comp_name K_AT int32",
    "manifestres_decl : D_ASSEMBLY K_EXTERN slashed_name",
    "manifestres_decl : customattr_decl",
    "comp_qstring : QSTRING",
    "comp_qstring : comp_qstring PLUS QSTRING",
    "int32 : INT64",
    "int64 : INT64",
    "float64 : FLOAT64",
    "float64 : K_FLOAT32 OPEN_PARENS INT32 CLOSE_PARENS",
    "float64 : K_FLOAT32 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT32 CLOSE_PARENS",
    "hexbyte : HEXBYTE",
    "$$2 :",
    "bytes_list : OPEN_PARENS $$2 bytes CLOSE_PARENS",
    "bytes :",
    "bytes : hexbytes",
    "hexbytes : hexbyte",
    "hexbytes : hexbytes hexbyte",
    "truefalse : K_TRUE",
    "truefalse : K_FALSE",
    "id : ID",
    "id : SQSTRING",
    "comp_name : id",
    "comp_name : comp_name DOT comp_name",
    "comp_name : COMP_NAME",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static readonly string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,null,null,"'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,
    "'['",null,"']'",null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,"'{'",null,"'}'",null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    "EOF","ID","QSTRING","SQSTRING","COMP_NAME","INT32","INT64","FLOAT64",
    "HEXBYTE","DOT","OPEN_BRACE","CLOSE_BRACE","OPEN_BRACKET",
    "CLOSE_BRACKET","OPEN_PARENS","CLOSE_PARENS","COMMA","COLON",
    "DOUBLE_COLON","\"::\"","SEMICOLON","ASSIGN","STAR","AMPERSAND",
    "PLUS","SLASH","BANG","ELLIPSIS","\"...\"","DASH",
    "OPEN_ANGLE_BRACKET","CLOSE_ANGLE_BRACKET","UNKNOWN","INSTR_NONE",
    "INSTR_VAR","INSTR_I","INSTR_I8","INSTR_R","INSTR_BRTARGET",
    "INSTR_METHOD","INSTR_NEWOBJ","INSTR_FIELD","INSTR_TYPE",
    "INSTR_STRING","INSTR_SIG","INSTR_RVA","INSTR_TOK","INSTR_SWITCH",
    "INSTR_PHI","INSTR_LOCAL","INSTR_PARAM","D_ADDON","D_ALGORITHM",
    "D_ASSEMBLY","D_BACKING","D_BLOB","D_CAPABILITY","D_CCTOR","D_CLASS",
    "D_COMTYPE","D_CONFIG","D_IMAGEBASE","D_CORFLAGS","D_CTOR","D_CUSTOM",
    "D_DATA","D_EMITBYTE","D_ENTRYPOINT","D_EVENT","D_EXELOC","D_EXPORT",
    "D_FIELD","D_FILE","D_FIRE","D_GET","D_HASH","D_IMPLICITCOM",
    "D_LANGUAGE","D_LINE","D_XLINE","D_LOCALE","D_LOCALS","D_MANIFESTRES",
    "D_MAXSTACK","D_METHOD","D_MIME","D_MODULE","D_MRESOURCE",
    "D_NAMESPACE","D_ORIGINATOR","D_OS","D_OTHER","D_OVERRIDE","D_PACK",
    "D_PARAM","D_PERMISSION","D_PERMISSIONSET","D_PROCESSOR","D_PROPERTY",
    "D_PUBLICKEY","D_PUBLICKEYTOKEN","D_REMOVEON","D_SET","D_SIZE",
    "D_STACKRESERVE","D_SUBSYSTEM","D_TITLE","D_TRY","D_VER","D_VTABLE",
    "D_VTENTRY","D_VTFIXUP","D_ZEROINIT","K_AT","K_AS","K_IMPLICITCOM",
    "K_IMPLICITRES","K_NOAPPDOMAIN","K_NOPROCESS","K_NOMACHINE",
    "K_EXTERN","K_INSTANCE","K_EXPLICIT","K_DEFAULT","K_VARARG",
    "K_UNMANAGED","K_CDECL","K_STDCALL","K_THISCALL","K_FASTCALL",
    "K_MARSHAL","K_IN","K_OUT","K_OPT","K_STATIC","K_PUBLIC","K_PRIVATE",
    "K_FAMILY","K_INITONLY","K_RTSPECIALNAME","K_STRICT","K_SPECIALNAME",
    "K_ASSEMBLY","K_FAMANDASSEM","K_FAMORASSEM","K_PRIVATESCOPE",
    "K_LITERAL","K_NOTSERIALIZED","K_VALUE","K_NOT_IN_GC_HEAP",
    "K_INTERFACE","K_SEALED","K_ABSTRACT","K_AUTO","K_SEQUENTIAL",
    "K_ANSI","K_UNICODE","K_AUTOCHAR","K_BESTFIT","K_IMPORT",
    "K_SERIALIZABLE","K_NESTED","K_LATEINIT","K_EXTENDS","K_IMPLEMENTS",
    "K_FINAL","K_VIRTUAL","K_HIDEBYSIG","K_NEWSLOT","K_UNMANAGEDEXP",
    "K_PINVOKEIMPL","K_NOMANGLE","K_OLE","K_LASTERR","K_WINAPI",
    "K_NATIVE","K_IL","K_CIL","K_OPTIL","K_MANAGED","K_FORWARDREF",
    "K_RUNTIME","K_INTERNALCALL","K_SYNCHRONIZED","K_NOINLINING",
    "K_CUSTOM","K_FIXED","K_SYSSTRING","K_ARRAY","K_VARIANT","K_CURRENCY",
    "K_SYSCHAR","K_VOID","K_BOOL","K_INT8","K_INT16","K_INT32","K_INT64",
    "K_FLOAT32","K_FLOAT64","K_ERROR","K_UNSIGNED","K_UINT","K_UINT8",
    "K_UINT16","K_UINT32","K_UINT64","K_DECIMAL","K_DATE","K_BSTR",
    "K_LPSTR","K_LPWSTR","K_LPTSTR","K_OBJECTREF","K_IUNKNOWN",
    "K_IDISPATCH","K_STRUCT","K_SAFEARRAY","K_INT","K_BYVALSTR","K_TBSTR",
    "K_LPVOID","K_ANY","K_FLOAT","K_LPSTRUCT","K_NULL","K_PTR","K_VECTOR",
    "K_HRESULT","K_CARRAY","K_USERDEFINED","K_RECORD","K_FILETIME",
    "K_BLOB","K_STREAM","K_STORAGE","K_STREAMED_OBJECT","K_STORED_OBJECT",
    "K_BLOB_OBJECT","K_CF","K_CLSID","K_METHOD","K_CLASS","K_PINNED",
    "K_MODREQ","K_MODOPT","K_TYPEDREF","K_TYPE","K_WCHAR","K_CHAR",
    "K_FROMUNMANAGED","K_CALLMOSTDERIVED","K_BYTEARRAY","K_WITH","K_INIT",
    "K_TO","K_CATCH","K_FILTER","K_FINALLY","K_FAULT","K_HANDLER","K_TLS",
    "K_FIELD","K_PROPERTY","K_REQUEST","K_DEMAND","K_ASSERT","K_DENY",
    "K_PERMITONLY","K_LINKCHECK","K_INHERITCHECK","K_REQMIN","K_REQOPT",
    "K_REQREFUSE","K_PREJITGRANT","K_PREJITDENY","K_NONCASDEMAND",
    "K_NONCASLINKDEMAND","K_NONCASINHERITANCE","K_READONLY",
    "K_NOMETADATA","K_ALGORITHM","K_FULLORIGIN","K_ENABLEJITTRACKING",
    "K_DISABLEJITOPTIMIZER","K_RETARGETABLE","K_PRESERVESIG",
    "K_BEFOREFIELDINIT","K_ALIGNMENT","K_NULLREF","K_VALUETYPE",
    "K_COMPILERCONTROLLED","K_REQSECOBJ","K_ENUM","K_OBJECT","K_STRING",
    "K_TRUE","K_FALSE","K_IS","K_ON","K_OFF","K_FORWARDER",
    "K_CHARMAPERROR",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

#pragma warning disable 414
  int yyExpectingState;
#pragma warning restore 414
  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected int [] yyExpectingTokens (int state){
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];
    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    int [] result = new int [len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = token;
    return result;
  }
  protected string[] yyExpecting (int state) {
    int [] tokens = yyExpectingTokens (state);
    string [] result = new string[tokens.Length];
    for (int n = 0; n < tokens.Length;  n++)
      result[n++] = yyNames[tokens [n]];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

	static int[] global_yyStates;
	static object[] global_yyVals;
#pragma warning disable 649
	protected bool use_global_stacks;
#pragma warning restore 649
	object[] yyVals;					// value stack
	object yyVal;						// value stack ptr
	int yyToken;						// current input
	int yyTop;

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;		// initial size
    int yyState = 0;                   // state stack ptr
    int [] yyStates;               	// state stack 
    yyVal = null;
    yyToken = -1;
    int yyErrorFlag = 0;				// #tks to shift
	if (use_global_stacks && global_yyStates != null) {
		yyVals = global_yyVals;
		yyStates = global_yyStates;
   } else {
		yyVals = new object [yyMax];
		yyStates = new int [yyMax];
		if (use_global_stacks) {
			global_yyVals = yyVals;
			global_yyStates = yyStates;
		}
	}

    /*yyLoop:*/ for (yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        global::System.Array.Resize (ref yyStates, yyStates.Length+yyMax);
        global::System.Array.Resize (ref yyVals, yyVals.Length+yyMax);
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ while (true) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              yyExpectingState = yyState;
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              if (yyToken == 0 /*eof*/ || yyToken == eof_token) throw new yyParser.yyUnexpectedEof ();
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyV > yyTop ? null : yyVals[yyV]; // yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 17:
  case_17();
  break;
case 18:
#line 522 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetSubSystem ((int) yyVals[0+yyTop]);
                          }
  break;
case 19:
#line 526 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetCorFlags ((int) yyVals[0+yyTop]);
                          }
  break;
case 21:
#line 531 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetImageBase ((long) yyVals[0+yyTop]);
                          }
  break;
case 22:
#line 535 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.SetStackReserve ((long)	yyVals[0+yyTop]);
                          }
  break;
case 38:
#line 566 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentNameSpace = null;
                          }
  break;
case 39:
#line 572 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentNameSpace = (string) yyVals[0+yyTop];
                          }
  break;
case 40:
#line 578 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.EndTypeDef ();
                          }
  break;
case 41:
  case_41();
  break;
case 43:
  case_43();
  break;
case 44:
#line 604 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Public; }
  break;
case 45:
#line 605 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Private; }
  break;
case 46:
#line 606 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPrivate; }
  break;
case 47:
#line 607 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPublic; }
  break;
case 48:
#line 608 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamily; }
  break;
case 49:
#line 609 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedAssembly;}
  break;
case 50:
#line 610 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamAndAssem; }
  break;
case 51:
#line 611 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamOrAssem; }
  break;
case 52:
#line 612 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { is_value_class = true; }
  break;
case 53:
#line 614 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { is_enum_class = true; is_value_class = true;
			  }
  break;
case 54:
#line 615 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Interface; }
  break;
case 55:
#line 616 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Sealed; }
  break;
case 56:
#line 617 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Abstract; }
  break;
case 57:
#line 618 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {  }
  break;
case 58:
#line 619 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.SequentialLayout; }
  break;
case 59:
#line 620 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.ExplicitLayout; }
  break;
case 60:
#line 621 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {  }
  break;
case 61:
#line 622 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.UnicodeClass; }
  break;
case 62:
#line 623 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.AutoClass; }
  break;
case 63:
#line 624 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Import; }
  break;
case 64:
#line 625 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Serializable; }
  break;
case 65:
#line 626 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.BeforeFieldInit; }
  break;
case 66:
#line 627 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.SpecialName; }
  break;
case 67:
#line 628 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.RTSpecialName; }
  break;
case 69:
#line 635 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 72:
  case_72();
  break;
case 73:
  case_73();
  break;
case 75:
#line 661 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 77:
#line 668 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 78:
  case_78();
  break;
case 79:
  case_79();
  break;
case 81:
#line 688 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 82:
  case_82();
  break;
case 83:
  case_83();
  break;
case 84:
#line 709 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
			  }
  break;
case 85:
  case_85();
  break;
case 86:
  case_86();
  break;
case 87:
  case_87();
  break;
case 88:
  case_88();
  break;
case 89:
  case_89();
  break;
case 90:
  case_90();
  break;
case 91:
  case_91();
  break;
case 92:
#line 766 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PEAPI.GenericParamAttributes ();
                          }
  break;
case 93:
#line 770 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.Covariant; 
                          }
  break;
case 94:
#line 774 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.Contravariant; 
                          }
  break;
case 95:
#line 778 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.DefaultConstructorConstrait; 
                          }
  break;
case 96:
#line 782 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.NotNullableValueTypeConstraint; 
                          }
  break;
case 97:
#line 786 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.ReferenceTypeConstraint; 
                          }
  break;
case 98:
#line 792 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 99:
  case_99();
  break;
case 100:
  case_100();
  break;
case 101:
  case_101();
  break;
case 102:
  case_102();
  break;
case 104:
#line 833 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = String.Format ("{0}/{1}", yyVals[-2+yyTop], yyVals[0+yyTop]);
                          }
  break;
case 105:
  case_105();
  break;
case 106:
  case_106();
  break;
case 107:
  case_107();
  break;
case 116:
#line 877 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], false);
			  }
  break;
case 118:
  case_118();
  break;
case 120:
#line 888 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.SetSize ((int) yyVals[0+yyTop]);
                          }
  break;
case 121:
#line 892 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.SetPack ((int) yyVals[0+yyTop]);
                          }
  break;
case 122:
  case_122();
  break;
case 125:
#line 925 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
			  	yyVal = yyVals[0+yyTop];
                          }
  break;
case 126:
#line 929 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Object, "System.Object");
                          }
  break;
case 127:
  case_127();
  break;
case 128:
  case_128();
  break;
case 129:
  case_129();
  break;
case 130:
  case_130();
  break;
case 131:
  case_131();
  break;
case 132:
  case_132();
  break;
case 133:
  case_133();
  break;
case 134:
  case_134();
  break;
case 135:
  case_135();
  break;
case 136:
  case_136();
  break;
case 137:
#line 1003 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new MethodPointerTypeRef ((CallConv) yyVals[-5+yyTop], (BaseTypeRef) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
                          }
  break;
case 139:
#line 1012 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int8, "System.SByte");
                          }
  break;
case 140:
#line 1016 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int16, "System.Int16");
                          }
  break;
case 141:
#line 1020 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int32, "System.Int32");
                          }
  break;
case 142:
#line 1024 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int64, "System.Int64");
                          }
  break;
case 143:
#line 1028 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Float32, "System.Single");
                          }
  break;
case 144:
#line 1032 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Float64, "System.Double");
                          }
  break;
case 145:
#line 1036 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt8, "System.Byte");
                          }
  break;
case 146:
#line 1040 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt8, "System.Byte");
                          }
  break;
case 147:
#line 1044 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt16, "System.UInt16");     
                          }
  break;
case 148:
#line 1048 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt16, "System.UInt16");     
                          }
  break;
case 149:
#line 1052 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt32, "System.UInt32");
                          }
  break;
case 150:
#line 1056 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt32, "System.UInt32");
                          }
  break;
case 151:
#line 1060 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt64, "System.UInt64");
                          }
  break;
case 152:
#line 1064 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt64, "System.UInt64");
                          }
  break;
case 153:
  case_153();
  break;
case 154:
#line 1073 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeUInt, "System.UIntPtr");
                          }
  break;
case 155:
#line 1077 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeUInt, "System.UIntPtr");
                          }
  break;
case 156:
  case_156();
  break;
case 157:
#line 1086 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Char, "System.Char");
                          }
  break;
case 158:
#line 1090 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new PrimitiveTypeRef (PrimitiveType.Char, "System.Char");
			  }
  break;
case 159:
#line 1094 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Void, "System.Void");
                          }
  break;
case 160:
#line 1098 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Boolean, "System.Boolean");
                          }
  break;
case 161:
#line 1102 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.String, "System.String");
                          }
  break;
case 162:
  case_162();
  break;
case 163:
  case_163();
  break;
case 164:
  case_164();
  break;
case 165:
  case_165();
  break;
case 166:
  case_166();
  break;
case 167:
  case_167();
  break;
case 168:
  case_168();
  break;
case 169:
#line 1159 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (CallConv) yyVals[0+yyTop] | CallConv.Instance;
                          }
  break;
case 170:
#line 1163 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (CallConv) yyVals[0+yyTop] | CallConv.InstanceExplicit;
                          }
  break;
case 172:
#line 1170 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CallConv ();
                          }
  break;
case 173:
#line 1174 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Default;
                          }
  break;
case 174:
#line 1178 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Vararg;
                          }
  break;
case 175:
#line 1182 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Cdecl;
                          }
  break;
case 176:
#line 1186 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Stdcall;
                          }
  break;
case 177:
#line 1190 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Thiscall;
                          }
  break;
case 178:
#line 1194 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Fastcall;
                          }
  break;
case 180:
#line 1201 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new CustomMarshaller ((string) yyVals[-3+yyTop], (string) yyVals[-1+yyTop]);
			  }
  break;
case 181:
#line 1205 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FixedSysString ((uint) (int)yyVals[-1+yyTop]);
                          }
  break;
case 182:
#line 1209 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FixedArray ((int) yyVals[-1+yyTop]);        
                          }
  break;
case 184:
#line 1214 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Currency;
                          }
  break;
case 186:
#line 1219 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Void;
                          }
  break;
case 187:
#line 1223 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Boolean;
                          }
  break;
case 188:
#line 1227 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int8;
                          }
  break;
case 189:
#line 1231 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int16;
                          }
  break;
case 190:
#line 1235 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int32;
                          }
  break;
case 191:
#line 1239 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int64;
                          }
  break;
case 192:
#line 1243 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Float32;
                          }
  break;
case 193:
#line 1247 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Float64;
                          }
  break;
case 194:
#line 1251 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Error;
                          }
  break;
case 195:
#line 1255 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt8;
                          }
  break;
case 196:
#line 1259 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt8;
                          }
  break;
case 197:
#line 1263 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt16;
                          }
  break;
case 198:
#line 1267 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt16;
                          }
  break;
case 199:
#line 1271 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt32;
                          }
  break;
case 200:
#line 1275 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt32;
                          }
  break;
case 201:
#line 1279 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt64;
                          }
  break;
case 202:
#line 1283 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt64;
                          }
  break;
case 204:
#line 1288 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new NativeArray ((NativeType) yyVals[-2+yyTop]);
			  }
  break;
case 205:
#line 1292 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                		yyVal = new NativeArray ((NativeType) yyVals[-3+yyTop], (int) yyVals[-1+yyTop], 0, 0);
			  }
  break;
case 206:
  case_206();
  break;
case 207:
  case_207();
  break;
case 210:
#line 1308 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.BStr;
                          }
  break;
case 211:
#line 1312 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPStr;
                          }
  break;
case 212:
#line 1316 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPWStr;
                          }
  break;
case 213:
#line 1320 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPTStr;
                          }
  break;
case 215:
#line 1325 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.IUnknown;
                          }
  break;
case 216:
#line 1329 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.IDispatch;
                          }
  break;
case 217:
#line 1333 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Struct;
                          }
  break;
case 218:
#line 1337 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Interface;
                          }
  break;
case 219:
  case_219();
  break;
case 221:
#line 1349 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int;
                          }
  break;
case 222:
#line 1353 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt;
                          }
  break;
case 224:
#line 1358 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.ByValStr;
                          }
  break;
case 225:
#line 1362 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.AnsiBStr;
                          }
  break;
case 226:
#line 1366 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.TBstr;
                          }
  break;
case 227:
#line 1370 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.VariantBool;
                          }
  break;
case 228:
#line 1374 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.FuncPtr;
                          }
  break;
case 229:
#line 1378 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.AsAny;
                          }
  break;
case 230:
#line 1382 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPStruct;
                          }
  break;
case 233:
#line 1390 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.variant;
			  }
  break;
case 234:
#line 1394 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.currency;
			  }
  break;
case 236:
#line 1399 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.boolean;
			  }
  break;
case 237:
#line 1403 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int8;
			  }
  break;
case 238:
#line 1407 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int16;
			  }
  break;
case 239:
#line 1411 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int32;
			  }
  break;
case 241:
#line 1416 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.float32;
			  }
  break;
case 242:
#line 1420 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.float64;
			  }
  break;
case 243:
#line 1424 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint8;
			  }
  break;
case 244:
#line 1428 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint16;
			  }
  break;
case 245:
#line 1432 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint32;
			  }
  break;
case 251:
#line 1441 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.Decimal;
			  }
  break;
case 252:
#line 1445 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.date;
			  }
  break;
case 253:
#line 1449 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.bstr;
			  }
  break;
case 256:
#line 1455 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.unknown;
			  }
  break;
case 257:
#line 1459 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.unknown;
			  }
  break;
case 259:
#line 1464 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.Int;
			  }
  break;
case 260:
#line 1468 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.UInt;
			  }
  break;
case 261:
#line 1472 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.error;
			  }
  break;
case 275:
  case_275();
  break;
case 277:
#line 1513 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 278:
#line 1519 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FieldAttr ();
                          }
  break;
case 279:
#line 1523 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Public;
                          }
  break;
case 280:
#line 1527 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Private;
                          }
  break;
case 281:
#line 1531 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Family;
                          }
  break;
case 282:
#line 1535 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Assembly;
                          }
  break;
case 283:
#line 1539 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.FamAndAssem;
                          }
  break;
case 284:
#line 1543 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.FamOrAssem;
                          }
  break;
case 285:
#line 1547 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                /* This is just 0x0000*/
                          }
  break;
case 286:
#line 1551 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Static;
                          }
  break;
case 287:
#line 1555 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Initonly;
                          }
  break;
case 288:
#line 1559 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.RTSpecialName;
                          }
  break;
case 289:
#line 1563 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.SpecialName;
                          }
  break;
case 290:
  case_290();
  break;
case 291:
#line 1572 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Literal;
                          }
  break;
case 292:
#line 1576 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Notserialized;
                          }
  break;
case 294:
#line 1583 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 296:
#line 1590 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 297:
#line 1596 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FloatConst (Convert.ToSingle (yyVals[-1+yyTop]));
                          }
  break;
case 298:
#line 1600 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DoubleConst (Convert.ToDouble (yyVals[-1+yyTop]));
                          }
  break;
case 299:
#line 1604 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FloatConst (BitConverter.ToSingle (BitConverter.GetBytes ((long)yyVals[-1+yyTop]), BitConverter.IsLittleEndian ? 0 : 4));
                          }
  break;
case 300:
#line 1608 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DoubleConst (BitConverter.Int64BitsToDouble ((long)yyVals[-1+yyTop]));
                          }
  break;
case 301:
#line 1612 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst (Convert.ToInt64 (yyVals[-1+yyTop]));
                          }
  break;
case 302:
#line 1616 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst (Convert.ToUInt64 ((ulong)(long) yyVals[-1+yyTop]));
                          }
  break;
case 303:
#line 1620 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((int)((long)yyVals[-1+yyTop]));
                          }
  break;
case 304:
#line 1624 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((uint)((long)yyVals[-1+yyTop]));
                          }
  break;
case 305:
#line 1628 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((short)((long) yyVals[-1+yyTop]));
                          }
  break;
case 306:
#line 1632 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((ushort)((long) yyVals[-1+yyTop]));
                          }
  break;
case 307:
#line 1636 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CharConst (Convert.ToChar (yyVals[-1+yyTop]));
                          }
  break;
case 308:
#line 1640 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new CharConst (Convert.ToChar (yyVals[-1+yyTop]));
			  }
  break;
case 309:
#line 1644 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((sbyte)((long) (yyVals[-1+yyTop])));
                          }
  break;
case 310:
#line 1648 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((byte)((long) (yyVals[-1+yyTop])));
                          }
  break;
case 311:
#line 1652 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new BoolConst ((bool) yyVals[-1+yyTop]);
                          }
  break;
case 313:
#line 1659 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }
  break;
case 314:
  case_314();
  break;
case 315:
#line 1668 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new NullConst ();
                          }
  break;
case 316:
  case_316();
  break;
case 317:
#line 1693 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DataDef ((string) yyVals[-1+yyTop], (bool) yyVals[-2+yyTop]);    
                          }
  break;
case 318:
#line 1697 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DataDef (String.Empty, (bool) yyVals[0+yyTop]);
                          }
  break;
case 319:
#line 1700 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = false; }
  break;
case 320:
#line 1701 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = true; }
  break;
case 321:
#line 1707 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 323:
  case_323();
  break;
case 324:
  case_324();
  break;
case 325:
#line 1727 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new StringConst ((string) yyVals[-1+yyTop]);
                          }
  break;
case 326:
#line 1731 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new StringConst ((string) yyVals[-1+yyTop]);
			  }
  break;
case 327:
  case_327();
  break;
case 328:
#line 1740 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }
  break;
case 329:
  case_329();
  break;
case 330:
  case_330();
  break;
case 331:
  case_331();
  break;
case 332:
  case_332();
  break;
case 333:
  case_333();
  break;
case 334:
  case_334();
  break;
case 335:
  case_335();
  break;
case 336:
  case_336();
  break;
case 337:
  case_337();
  break;
case 338:
  case_338();
  break;
case 339:
  case_339();
  break;
case 340:
  case_340();
  break;
case 341:
  case_341();
  break;
case 342:
#line 1862 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.EndMethodDef (tokenizer.Location);
                          }
  break;
case 343:
  case_343();
  break;
case 344:
  case_344();
  break;
case 345:
#line 1901 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new MethAttr (); }
  break;
case 346:
#line 1902 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Static; }
  break;
case 347:
#line 1903 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Public; }
  break;
case 348:
#line 1904 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Private; }
  break;
case 349:
#line 1905 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Family; }
  break;
case 350:
#line 1906 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Assembly; }
  break;
case 351:
#line 1907 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.FamAndAssem; }
  break;
case 352:
#line 1908 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.FamOrAssem; }
  break;
case 353:
#line 1909 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { /* CHECK HEADERS */ }
  break;
case 354:
#line 1910 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Final; }
  break;
case 355:
#line 1911 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Virtual; }
  break;
case 356:
#line 1912 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Abstract; }
  break;
case 357:
#line 1913 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.HideBySig; }
  break;
case 358:
#line 1914 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.NewSlot; }
  break;
case 359:
#line 1915 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.RequireSecObject; }
  break;
case 360:
#line 1916 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.SpecialName; }
  break;
case 361:
#line 1917 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.RTSpecialName; }
  break;
case 362:
#line 1918 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Strict; }
  break;
case 363:
#line 1919 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { /* Do nothing */ }
  break;
case 365:
  case_365();
  break;
case 366:
  case_366();
  break;
case 367:
  case_367();
  break;
case 368:
#line 1945 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new PInvokeAttr (); }
  break;
case 369:
#line 1946 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.nomangle; }
  break;
case 370:
#line 1947 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.ansi; }
  break;
case 371:
#line 1948 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.unicode; }
  break;
case 372:
#line 1949 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.autochar; }
  break;
case 373:
#line 1950 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.lasterr; }
  break;
case 374:
#line 1951 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.winapi; }
  break;
case 375:
#line 1952 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.cdecl; }
  break;
case 376:
#line 1953 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.stdcall; }
  break;
case 377:
#line 1954 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.thiscall; }
  break;
case 378:
#line 1955 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.fastcall; }
  break;
case 379:
#line 1956 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.bestfit_on; }
  break;
case 380:
#line 1957 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.bestfit_off; }
  break;
case 381:
#line 1958 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.charmaperror_on; }
  break;
case 382:
#line 1959 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.charmaperror_off; }
  break;
case 386:
#line 1967 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new ParamAttr (); }
  break;
case 387:
#line 1968 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.In; }
  break;
case 388:
#line 1969 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.Out; }
  break;
case 389:
#line 1970 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.Opt; }
  break;
case 390:
#line 1973 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new ImplAttr (); }
  break;
case 391:
#line 1974 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Native; }
  break;
case 392:
#line 1975 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.IL; }
  break;
case 393:
#line 1976 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.IL; }
  break;
case 394:
#line 1977 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Optil; }
  break;
case 395:
#line 1978 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { /* should this reset? */ }
  break;
case 396:
#line 1979 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Unmanaged; }
  break;
case 397:
#line 1980 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.ForwardRef; }
  break;
case 398:
#line 1981 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.PreserveSig; }
  break;
case 399:
#line 1982 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Runtime; }
  break;
case 400:
#line 1983 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.InternalCall; }
  break;
case 401:
#line 1984 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Synchronised; }
  break;
case 402:
#line 1985 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.NoInLining; }
  break;
case 405:
  case_405();
  break;
case 406:
  case_406();
  break;
case 407:
#line 2009 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ParamDef ((ParamAttr) yyVals[-1+yyTop], null, (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 408:
#line 2013 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ParamDef ((ParamAttr) yyVals[-2+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 409:
  case_409();
  break;
case 410:
  case_410();
  break;
case 411:
  case_411();
  break;
case 412:
#line 2038 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ArrayList (0);
                          }
  break;
case 413:
  case_413();
  break;
case 414:
  case_414();
  break;
case 415:
  case_415();
  break;
case 416:
  case_416();
  break;
case 421:
  case_421();
  break;
case 422:
#line 2083 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.SetMaxStack ((int) yyVals[0+yyTop]);
                          }
  break;
case 423:
  case_423();
  break;
case 424:
  case_424();
  break;
case 425:
  case_425();
  break;
case 426:
#line 2107 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.ZeroInit ();
                          }
  break;
case 430:
  case_430();
  break;
case 431:
  case_431();
  break;
case 432:
  case_432();
  break;
case 434:
  case_434();
  break;
case 436:
#line 2163 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.AddLabel ((string) yyVals[-1+yyTop]);
                          }
  break;
case 439:
#line 2169 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], false);
			  }
  break;
case 442:
  case_442();
  break;
case 445:
  case_445();
  break;
case 446:
  case_446();
  break;
case 447:
#line 2197 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local (-1, (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 448:
#line 2201 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local (-1, (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 449:
#line 2205 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local ((int) yyVals[-1+yyTop], (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 450:
#line 2209 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local ((int) yyVals[-2+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 451:
#line 2215 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 452:
  case_452();
  break;
case 453:
  case_453();
  break;
case 455:
  case_455();
  break;
case 456:
  case_456();
  break;
case 457:
  case_457();
  break;
case 458:
#line 2268 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new TryBlock ((HandlerBlock) yyVals[0+yyTop], tokenizer.Location);
                          }
  break;
case 459:
  case_459();
  break;
case 460:
  case_460();
  break;
case 461:
  case_461();
  break;
case 462:
  case_462();
  break;
case 463:
  case_463();
  break;
case 464:
  case_464();
  break;
case 465:
  case_465();
  break;
case 466:
  case_466();
  break;
case 467:
  case_467();
  break;
case 468:
  case_468();
  break;
case 469:
  case_469();
  break;
case 470:
#line 2350 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                
                          }
  break;
case 471:
  case_471();
  break;
case 472:
  case_472();
  break;
case 473:
  case_473();
  break;
case 474:
  case_474();
  break;
case 475:
  case_475();
  break;
case 476:
  case_476();
  break;
case 477:
  case_477();
  break;
case 478:
  case_478();
  break;
case 479:
  case_479();
  break;
case 480:
  case_480();
  break;
case 481:
  case_481();
  break;
case 482:
  case_482();
  break;
case 483:
  case_483();
  break;
case 484:
  case_484();
  break;
case 485:
  case_485();
  break;
case 486:
  case_486();
  break;
case 487:
  case_487();
  break;
case 488:
  case_488();
  break;
case 489:
  case_489();
  break;
case 490:
  case_490();
  break;
case 491:
  case_491();
  break;
case 492:
  case_492();
  break;
case 493:
  case_493();
  break;
case 494:
  case_494();
  break;
case 495:
#line 2548 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.AddInstr (new SwitchInstr ((ArrayList) yyVals[-1+yyTop], tokenizer.Location));
                          }
  break;
case 496:
  case_496();
  break;
case 497:
  case_497();
  break;
case 499:
  case_499();
  break;
case 500:
  case_500();
  break;
case 501:
  case_501();
  break;
case 502:
  case_502();
  break;
case 505:
#line 2638 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 506:
  case_506();
  break;
case 507:
#line 2649 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = codegen.GetGlobalFieldRef ((BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
                          }
  break;
case 508:
#line 2655 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.EndEventDef ();
                          }
  break;
case 509:
  case_509();
  break;
case 511:
#line 2671 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FeatureAttr ();
                          }
  break;
case 512:
#line 2675 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] & FeatureAttr.Rtspecialname;
                          }
  break;
case 513:
#line 2679 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] & FeatureAttr.Specialname;
                          }
  break;
case 516:
  case_516();
  break;
case 517:
  case_517();
  break;
case 518:
  case_518();
  break;
case 519:
  case_519();
  break;
case 520:
  case_520();
  break;
case 523:
#line 2718 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.EndPropertyDef ();
                          }
  break;
case 524:
  case_524();
  break;
case 525:
#line 2737 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FeatureAttr ();
                          }
  break;
case 526:
#line 2741 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Rtspecialname;
                          }
  break;
case 527:
#line 2745 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Specialname;
                          }
  break;
case 528:
#line 2749 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Instance;
                          }
  break;
case 531:
#line 2759 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddSet ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 532:
#line 2763 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddGet ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 533:
#line 2767 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddOther ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 534:
  case_534();
  break;
case 537:
#line 2780 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CustomAttr ((BaseMethodRef) yyVals[0+yyTop], null);
                          }
  break;
case 539:
  case_539();
  break;
case 543:
  case_543();
  break;
case 544:
  case_544();
  break;
case 545:
#line 2825 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = TypeSpecToPermPair (yyVals[-4+yyTop], yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 546:
#line 2829 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = TypeSpecToPermPair (yyVals[-1+yyTop], yyVals[0+yyTop], null);
			  }
  break;
case 547:
  case_547();
  break;
case 548:
  case_548();
  break;
case 549:
#line 2846 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new MIPermissionSet ((PEAPI.SecurityAction) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 550:
  case_550();
  break;
case 551:
  case_551();
  break;
case 552:
#line 2866 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new MIPermission ((BaseTypeRef) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 553:
  case_553();
  break;
case 554:
  case_554();
  break;
case 555:
  case_555();
  break;
case 556:
  case_556();
  break;
case 557:
#line 2898 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new NameValuePair ((string) yyVals[-2+yyTop], (PEAPI.Constant) yyVals[0+yyTop]);
			  }
  break;
case 558:
#line 2902 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new NameValuePair ((string) yyVals[-3+yyTop], new ByteArrConst ((byte[]) yyVals[0+yyTop]));
                          }
  break;
case 559:
#line 2906 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new NameValuePair ((string) yyVals[-5+yyTop], new StringConst ((string) yyVals[-1+yyTop]));
			  }
  break;
case 560:
#line 2912 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = MemberTypes.Property;
			  }
  break;
case 561:
#line 2916 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = MemberTypes.Field;
			  }
  break;
case 562:
  case_562();
  break;
case 563:
  case_563();
  break;
case 564:
#line 2936 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new NameValuePair ((string) yyVals[-2+yyTop], yyVals[0+yyTop]);
			  }
  break;
case 567:
#line 2944 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = yyVals[-1+yyTop];
			  }
  break;
case 569:
#line 2949 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (byte) (int) yyVals[-1+yyTop]);
			  }
  break;
case 570:
#line 2953 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (short) (int) yyVals[-1+yyTop]);
			  }
  break;
case 571:
#line 2957 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (int) yyVals[-1+yyTop]);
			  }
  break;
case 572:
#line 2961 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-3+yyTop], (int) yyVals[-1+yyTop]);
			  }
  break;
case 573:
#line 2967 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Request;
			  }
  break;
case 574:
#line 2971 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Demand;
			  }
  break;
case 575:
#line 2975 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Assert;
			  }
  break;
case 576:
#line 2979 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Deny;
			  }
  break;
case 577:
#line 2983 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PermitOnly;
			  }
  break;
case 578:
#line 2987 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.LinkDemand;
			  }
  break;
case 579:
#line 2991 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.InheritDemand;
			  }
  break;
case 580:
#line 2995 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestMinimum;
			  }
  break;
case 581:
#line 2999 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestOptional;
			  }
  break;
case 582:
#line 3003 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestRefuse;
			  }
  break;
case 583:
#line 3007 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PreJitGrant;
			  }
  break;
case 584:
#line 3011 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PreJitDeny;
			  }
  break;
case 585:
#line 3015 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasDemand;
			  }
  break;
case 586:
#line 3019 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasLinkDemand;
			  }
  break;
case 587:
#line 3023 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasInheritance;
			  }
  break;
case 588:
#line 3029 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                          }
  break;
case 589:
#line 3033 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetModuleName ((string) yyVals[0+yyTop]);
                          }
  break;
case 590:
#line 3037 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.ExternTable.AddModule ((string) yyVals[0+yyTop]);                         
                          }
  break;
case 591:
#line 3044 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetFileRef (new FileRef ((string) yyVals[-5+yyTop], (byte []) yyVals[-1+yyTop], (bool) yyVals[-6+yyTop], (bool) yyVals[0+yyTop])); 
                          }
  break;
case 592:
  case_592();
  break;
case 593:
#line 3055 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 594:
#line 3059 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 595:
#line 3065 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 596:
#line 3069 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 597:
  case_597();
  break;
case 598:
  case_598();
  break;
case 599:
#line 3090 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				  yyVal = new PEAPI.AssemAttr ();
			  }
  break;
case 600:
#line 3097 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				  yyVal = ((PEAPI.AssemAttr) yyVals[-1+yyTop]) | PEAPI.AssemAttr.Retargetable;
			  }
  break;
case 603:
#line 3107 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetPublicKey ((byte []) yyVals[0+yyTop]);
			  }
  break;
case 604:
#line 3111 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetVersion ((int) yyVals[-6+yyTop], (int) yyVals[-4+yyTop], (int) yyVals[-2+yyTop], (int) yyVals[0+yyTop]);
			  }
  break;
case 605:
#line 3115 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetLocale ((string) yyVals[0+yyTop]);
			  }
  break;
case 607:
#line 3120 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetHashAlgorithm ((int) yyVals[0+yyTop]);
			  }
  break;
case 608:
#line 3124 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
			  }
  break;
case 609:
#line 3128 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], true);
			  }
  break;
case 616:
  case_616();
  break;
case 617:
  case_617();
  break;
case 620:
#line 3164 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetVersion ((int) yyVals[-6+yyTop], (int) yyVals[-4+yyTop], (int) yyVals[-2+yyTop], (int) yyVals[0+yyTop]);
                          }
  break;
case 621:
#line 3168 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetPublicKey ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 622:
#line 3172 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetPublicKeyToken ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 623:
#line 3176 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetLocale ((string) yyVals[0+yyTop]);
                          }
  break;
case 625:
#line 3182 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetHash ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 626:
  case_626();
  break;
case 628:
#line 3196 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
						current_extern = new KeyValuePair<string, TypeAttr> ((string) yyVals[0+yyTop], (TypeAttr) yyVals[-1+yyTop]);
					}
  break;
case 629:
#line 3199 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = 0; }
  break;
case 630:
#line 3200 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Private; }
  break;
case 631:
#line 3201 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Public; }
  break;
case 632:
#line 3202 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPublic; }
  break;
case 633:
#line 3203 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPrivate; }
  break;
case 634:
#line 3204 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamily; }
  break;
case 635:
#line 3205 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedAssembly;}
  break;
case 636:
#line 3206 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamAndAssem; }
  break;
case 637:
#line 3207 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamOrAssem; }
  break;
case 638:
#line 3208 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = TypeAttr.Forwarder; }
  break;
case 644:
#line 3221 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
			  	codegen.ExternTable.AddClass (current_extern.Key, current_extern.Value, (string) yyVals[0+yyTop]);
			  }
  break;
case 646:
  case_646();
  break;
case 648:
#line 3239 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = ManifestResource.PublicResource; }
  break;
case 649:
#line 3240 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = ManifestResource.PrivateResource; }
  break;
case 656:
#line 3253 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = String.Format ("{0}{1}", yyVals[-2+yyTop], yyVals[0+yyTop]); }
  break;
case 657:
  case_657();
  break;
case 660:
  case_660();
  break;
case 661:
  case_661();
  break;
case 662:
  case_662();
  break;
case 663:
  case_663();
  break;
case 664:
#line 3292 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { }
  break;
case 665:
#line 3298 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                tokenizer.InByteArray = true;
                          }
  break;
case 666:
  case_666();
  break;
case 667:
#line 3306 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new byte[0]; }
  break;
case 668:
  case_668();
  break;
case 669:
  case_669();
  break;
case 670:
  case_670();
  break;
case 671:
#line 3330 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 672:
#line 3334 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 676:
#line 3345 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (string) yyVals[-2+yyTop] + '.' + (string) yyVals[0+yyTop];
                          }
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: ;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: ;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

/*
 All more than 3 lines long rules are wrapped into a method
*/
void case_17()
#line 515 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				if (codegen.CurrentCustomAttrTarget != null)
					codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
			  }

void case_41()
#line 583 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.BeginTypeDef ((TypeAttr) yyVals[-4+yyTop], (string) yyVals[-3+yyTop], 
						yyVals[-1+yyTop] as BaseClassRef, yyVals[0+yyTop] as ArrayList, null, (GenericParameters) yyVals[-2+yyTop]);
				
				if (is_value_class)
					codegen.CurrentTypeDef.MakeValueClass ();
				if (is_enum_class)
					codegen.CurrentTypeDef.MakeEnumClass ();
                          }

void case_43()
#line 598 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{ 
				/* Reset some flags*/
				is_value_class = false;
				is_enum_class = false;
				yyVal = new TypeAttr ();
			  }

void case_72()
#line 643 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = new ArrayList ();
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_73()
#line 649 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = (ArrayList) yyVals[-2+yyTop];

                                al.Insert (0, yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_78()
#line 672 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GenericArguments ga = new GenericArguments ();
                                ga.Add ((BaseTypeRef) yyVals[0+yyTop]);
                                yyVal = ga;
                          }

void case_79()
#line 678 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ((GenericArguments) yyVals[-2+yyTop]).Add ((BaseTypeRef) yyVals[0+yyTop]);
                                yyVal = yyVals[-2+yyTop];
                          }

void case_82()
#line 693 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = new ArrayList ();
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                           }

void case_83()
#line 699 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = (ArrayList) yyVals[-2+yyTop];
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_85()
#line 711 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[0+yyTop] != null)
                                        yyVal = ((BaseClassRef) yyVals[-1+yyTop]).GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = yyVals[-1+yyTop];
			  }

void case_86()
#line 718 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GenParam gpar = new GenParam ((int) yyVals[0+yyTop], "", GenParamType.Var);
                                yyVal = new GenericParamRef (gpar, yyVals[0+yyTop].ToString ());
                          }

void case_87()
#line 723 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GenParam gpar = new GenParam ((int) yyVals[0+yyTop], "", GenParamType.MVar);
                                yyVal = new GenericParamRef (gpar, yyVals[0+yyTop].ToString ());
                          }

void case_88()
#line 728 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				int num = -1;
				string name = (string) yyVals[0+yyTop];
				if (codegen.CurrentTypeDef != null)
					num = codegen.CurrentTypeDef.GetGenericParamNum (name);
				GenParam gpar = new GenParam (num, name, GenParamType.Var);
                                yyVal = new GenericParamRef (gpar, name);
                          }

void case_89()
#line 737 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				int num = -1;
				string name = (string) yyVals[0+yyTop];
				if (codegen.CurrentMethodDef != null)
					num = codegen.CurrentMethodDef.GetGenericParamNum (name);
				GenParam gpar = new GenParam (num, name, GenParamType.MVar);
                                yyVal = new GenericParamRef (gpar, name);
                          }

void case_90()
#line 748 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GenericParameter gp = new GenericParameter ((string) yyVals[0+yyTop], (PEAPI.GenericParamAttributes) yyVals[-2+yyTop], (ArrayList) yyVals[-1+yyTop]);

                                GenericParameters colln = new GenericParameters ();
                                colln.Add (gp);
                                yyVal = colln;
                          }

void case_91()
#line 756 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GenericParameters colln = (GenericParameters) yyVals[-4+yyTop];
                                colln.Add (new GenericParameter ((string) yyVals[0+yyTop], (PEAPI.GenericParamAttributes) yyVals[-2+yyTop], (ArrayList) yyVals[-1+yyTop]));
                                yyVal = colln;
                          }

void case_99()
#line 796 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				  if (codegen.CurrentMethodDef != null)
					  codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam ((string) yyVals[0+yyTop]);
				  else
					  codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam ((string) yyVals[0+yyTop]);
				  if (codegen.CurrentCustomAttrTarget == null)
					  Report.Error (String.Format ("Type parameter '{0}' undefined.", (string) yyVals[0+yyTop]));
			  }

void case_100()
#line 805 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				  int index = ((int) yyVals[-1+yyTop]);
				  if (codegen.CurrentMethodDef != null)
					  codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam (index - 1);
				  else
					  codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam (index - 1);
				  if (codegen.CurrentCustomAttrTarget == null)
					  Report.Error (String.Format ("Type parameter '{0}' index out of range.", index));
			  }

void case_101()
#line 817 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList class_list = new ArrayList ();
                                class_list.Add (yyVals[0+yyTop]);
                                yyVal = class_list; 
                          }

void case_102()
#line 823 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList class_list = (ArrayList) yyVals[-2+yyTop];
                                class_list.Add (yyVals[0+yyTop]);
                          }

void case_105()
#line 837 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.IsThisAssembly ((string) yyVals[-2+yyTop])) {
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                                } else {
                                        yyVal = codegen.ExternTable.GetTypeRef ((string) yyVals[-2+yyTop], (string) yyVals[0+yyTop], false);
                                }
                          }

void case_106()
#line 845 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.IsThisModule ((string) yyVals[-2+yyTop])) {
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                                } else {
                                        yyVal = codegen.ExternTable.GetModuleTypeRef ((string) yyVals[-2+yyTop], (string) yyVals[0+yyTop], false);
                                }
                          }

void case_107()
#line 853 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                PrimitiveTypeRef prim = PrimitiveTypeRef.GetPrimitiveType ((string) yyVals[0+yyTop]);

                                if (prim != null && !codegen.IsThisAssembly ("mscorlib"))
                                        yyVal = prim;
                                else
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                          }

void case_118()
#line 880 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_122()
#line 895 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /**/
                                /* My copy of the spec didn't have a type_list but*/
                                /* it seems pretty crucial*/
                                /**/
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-9+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[0+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef decl;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                decl = owner.GetMethodRef ((BaseTypeRef) yyVals[-4+yyTop],
                                        (CallConv) yyVals[-5+yyTop], (string) yyVals[-7+yyTop], param_list, 0);

				/* NOTICE: `owner' here might be wrong*/
                                string sig = MethodDef.CreateSignature (owner, (CallConv) yyVals[-5+yyTop], (string) yyVals[-1+yyTop],
                                                                        param_list, 0, false);
                                codegen.CurrentTypeDef.AddOverride (sig, decl);                                        
                          }

void case_127()
#line 931 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				BaseClassRef class_ref = (BaseClassRef) yyVals[0+yyTop];
				class_ref.MakeValueClass ();
                                yyVal = class_ref;
                          }

void case_128()
#line 937 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ExternTypeRef ext_ref = codegen.ExternTable.GetTypeRef ((string) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], true);
                                if (yyVals[0+yyTop] != null)
                                        yyVal = ext_ref.GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = ext_ref;
                          }

void case_129()
#line 945 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                TypeRef t_ref = codegen.GetTypeRef ((string) yyVals[-1+yyTop]);
                                t_ref.MakeValueClass ();
                                if (yyVals[0+yyTop] != null)
                                        yyVal = t_ref.GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = t_ref;
                          }

void case_130()
#line 954 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-2+yyTop]);
                                base_type.MakeArray ();
                                yyVal = base_type;
                          }

void case_131()
#line 960 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-3+yyTop]);
                                ArrayList bound_list = (ArrayList) yyVals[-1+yyTop];
                                base_type.MakeBoundArray (bound_list);
                                yyVal = base_type;
                          }

void case_132()
#line 967 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-1+yyTop]);
                                base_type.MakeManagedPointer ();
                                yyVal = base_type;
                          }

void case_133()
#line 973 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-1+yyTop]);
                                base_type.MakeUnmanagedPointer ();
                                yyVal = base_type;
                          }

void case_134()
#line 979 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-1+yyTop]);
                                base_type.MakePinned ();
                                yyVal = base_type;
                          }

void case_135()
#line 985 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-4+yyTop]);
                                BaseClassRef class_ref = (BaseClassRef) yyVals[-1+yyTop];
                                base_type.MakeCustomModified (codegen,
                                        CustomModifier.modreq, class_ref);
                                yyVal = base_type;
                          }

void case_136()
#line 993 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = GetTypeRef ((BaseTypeRef) yyVals[-4+yyTop]);
                                BaseClassRef class_ref = (BaseClassRef) yyVals[-1+yyTop];
                                base_type.MakeCustomModified (codegen,
                                        CustomModifier.modopt, class_ref);
                                yyVal = base_type;
                          }

void case_153()
#line 1066 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* TODO: Is this the proper full name*/
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeInt, "System.IntPtr");
                          }

void case_156()
#line 1079 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = new PrimitiveTypeRef (PrimitiveType.TypedRef,
                                        "System.TypedReference");
                          }

void case_162()
#line 1106 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList bound_list = new ArrayList ();
                                bound_list.Add (yyVals[0+yyTop]);
                                yyVal = bound_list;
                          }

void case_163()
#line 1112 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList bound_list = (ArrayList) yyVals[-2+yyTop];
                                bound_list.Add (yyVals[0+yyTop]);
                          }

void case_164()
#line 1119 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* This is shortref for no lowerbound or size*/
                                yyVal = new DictionaryEntry (TypeRef.Ellipsis, TypeRef.Ellipsis);
                          }

void case_165()
#line 1124 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* No lower bound or size*/
                                yyVal = new DictionaryEntry (TypeRef.Ellipsis, TypeRef.Ellipsis);
                          }

void case_166()
#line 1129 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* Only size specified */ 
                                int size = (int) yyVals[0+yyTop];
                                if (size < 0)
                                        /* size cannot be < 0, so emit as (0, ...)
                                           ilasm.net emits it like this */
                                        yyVal = new DictionaryEntry (0, TypeRef.Ellipsis);
                                else
                                        yyVal = new DictionaryEntry (TypeRef.Ellipsis, size);
                          }

void case_167()
#line 1140 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* lower and upper bound*/
                                int lower = (int) yyVals[-2+yyTop];
                                int upper = (int) yyVals[0+yyTop];
                                if (lower > upper) 
                                        Report.Error ("Lower bound " + lower + " must be <= upper bound " + upper);

                                yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[0+yyTop]);
                          }

void case_168()
#line 1150 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* Just lower bound*/
                                yyVal = new DictionaryEntry (yyVals[-1+yyTop], TypeRef.Ellipsis);
                          }

void case_206()
#line 1294 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /*FIXME: Allowed only for methods, !fields*/
                                yyVal = new NativeArray ((NativeType) yyVals[-5+yyTop], (int) yyVals[-3+yyTop], (int) yyVals[-1+yyTop]);
			  }

void case_207()
#line 1299 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /*FIXME: Allowed only for methods, !fields*/
                                yyVal = new NativeArray ((NativeType) yyVals[-4+yyTop], -1, (int) yyVals[-1+yyTop]);
			  }

void case_219()
#line 1339 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[0+yyTop] == null)
                                        yyVal = new SafeArray ();
                                else        
                                        yyVal = new SafeArray ((SafeArrayType) yyVals[0+yyTop]);
                          }

void case_275()
#line 1489 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                FieldDef field_def = new FieldDef((FieldAttr) yyVals[-4+yyTop], 
					(string) yyVals[-2+yyTop], (BaseTypeRef) yyVals[-3+yyTop]);
                                codegen.AddFieldDef (field_def);
                                codegen.CurrentCustomAttrTarget = field_def;
                                
                                if (yyVals[-5+yyTop] != null) {
                                        field_def.SetOffset ((uint) (int)yyVals[-5+yyTop]);
                                }

                                if (yyVals[-1+yyTop] != null) {
                                        field_def.AddDataValue ((string) yyVals[-1+yyTop]);
                                }

                                if (yyVals[0+yyTop] != null) {
                                        field_def.SetValue ((Constant) yyVals[0+yyTop]);
                                }
                          }

void case_290()
#line 1565 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                               codegen.AddFieldMarshalInfo ((NativeType) yyVals[-1+yyTop]);
                               yyVal = (FieldAttr) yyVals[-4+yyTop] | FieldAttr.HasFieldMarshal;
                          }

void case_314()
#line 1661 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* ******** THIS IS NOT IN THE DOCUMENTATION ******** //*/
                                yyVal = new StringConst ((string) yyVals[0+yyTop]);
                          }

void case_316()
#line 1672 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                DataDef datadef = (DataDef) yyVals[-1+yyTop];
                                
                                if (yyVals[0+yyTop] is ArrayList) {
                                        ArrayList const_list = (ArrayList) yyVals[0+yyTop];
                                        DataConstant[] const_arr = new DataConstant[const_list.Count];
                                        
                                        for (int i=0; i<const_arr.Length; i++)
                                                const_arr[i] = (DataConstant) const_list[i];

                                        datadef.PeapiConstant = new ArrayConstant (const_arr);
                                } else {
                                        datadef.PeapiConstant = (PEAPI.Constant) yyVals[0+yyTop];
                                }
                                codegen.AddDataDef (datadef);
                          }

void case_323()
#line 1712 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList dataitem_list = new ArrayList ();
                                dataitem_list.Add (yyVals[0+yyTop]);
                                yyVal = dataitem_list;
                          }

void case_324()
#line 1718 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList list = (ArrayList) yyVals[-2+yyTop];
                                list.Add (yyVals[0+yyTop]);
                          }

void case_327()
#line 1733 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                           /*     DataDef def = codegen.CurrentTypeDef.GetDataDef ((string) $3);*/
                           /*     $$ = new AddressConstant ((DataConstant) def.PeapiConstant);*/
                          }

void case_329()
#line 1742 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* ******** THIS IS NOT IN THE SPECIFICATION ******** //*/
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }

void case_330()
#line 1747 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                double d = (double) yyVals[-2+yyTop];
                                FloatConst float_const = new FloatConst ((float) d);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (float_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = float_const;
                          }

void case_331()
#line 1757 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                DoubleConst double_const = new DoubleConst ((double) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (double_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = double_const;
                          }

void case_332()
#line 1766 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((long) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_333()
#line 1775 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((int) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_334()
#line 1784 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-2+yyTop];
                                IntConst int_const = new IntConst ((short) i);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_335()
#line 1794 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-2+yyTop];
                                IntConst int_const = new IntConst ((sbyte) i);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_336()
#line 1804 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                FloatConst float_const = new FloatConst (0F);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (float_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = float_const;
                          }

void case_337()
#line 1813 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                DoubleConst double_const = new DoubleConst (0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (double_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = double_const;
                          }

void case_338()
#line 1822 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((long) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_339()
#line 1831 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((int) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_340()
#line 1840 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((short) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_341()
#line 1849 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((sbyte) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_343()
#line 1867 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                CallConv cc = (CallConv) yyVals[-8+yyTop];
                                if (yyVals[-4+yyTop] != null)
                                        cc |= CallConv.Generic;

                                MethodDef methdef = new MethodDef (
                                        codegen, (MethAttr) yyVals[-9+yyTop], cc,
                                        (ImplAttr) yyVals[0+yyTop], (string) yyVals[-5+yyTop], (BaseTypeRef) yyVals[-6+yyTop],
                                        (ArrayList) yyVals[-2+yyTop], tokenizer.Reader.Location, (GenericParameters) yyVals[-4+yyTop], codegen.CurrentTypeDef);
                                if (pinvoke_info) {
                                        ExternModule mod = codegen.ExternTable.AddModule (pinvoke_mod);
                                        methdef.AddPInvokeInfo (pinvoke_attr, mod, pinvoke_meth);
                                        pinvoke_info = false;
                                }
                          }

void case_344()
#line 1885 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                MethodDef methdef = new MethodDef (
                              		codegen, (MethAttr) yyVals[-12+yyTop], (CallConv) yyVals[-11+yyTop],
                                        (ImplAttr) yyVals[0+yyTop], (string) yyVals[-4+yyTop], (BaseTypeRef) yyVals[-9+yyTop],
                                        (ArrayList) yyVals[-2+yyTop], tokenizer.Reader.Location, null, codegen.CurrentTypeDef);

                                if (pinvoke_info) {
                                        ExternModule mod = codegen.ExternTable.AddModule (pinvoke_mod);
                                        methdef.AddPInvokeInfo (pinvoke_attr, mod, pinvoke_meth);
                                        pinvoke_info = false;
                                }
		                
                                methdef.AddRetTypeMarshalInfo ((NativeType) yyVals[-6+yyTop]);
			  }

void case_365()
#line 1923 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = (string) yyVals[-4+yyTop];
                                pinvoke_meth = (string) yyVals[-2+yyTop];
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_366()
#line 1930 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = (string) yyVals[-2+yyTop];
                                pinvoke_meth = null;
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_367()
#line 1937 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = null;
                                pinvoke_meth = null;
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_405()
#line 1993 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList sig_list = new ArrayList ();
                                sig_list.Add (yyVals[0+yyTop]);
                                yyVal = sig_list;
                          }

void case_406()
#line 1999 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList sig_list = (ArrayList) yyVals[-2+yyTop];
                                sig_list.Add (yyVals[0+yyTop]);
                                yyVal = sig_list;
                          }

void case_409()
#line 2015 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				yyVal = new ParamDef ((ParamAttr) 0, "...", new SentinelTypeRef ());
                                /* $$ = ParamDef.Ellipsis;*/
                          }

void case_410()
#line 2020 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ParamDef param_def = new ParamDef ((ParamAttr) yyVals[-5+yyTop], null, (BaseTypeRef) yyVals[-4+yyTop]);
                                param_def.AddMarshalInfo ((PEAPI.NativeType) yyVals[-1+yyTop]);

                                yyVal = param_def;
			  }

void case_411()
#line 2027 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ParamDef param_def = new ParamDef ((ParamAttr) yyVals[-6+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-5+yyTop]);
                                param_def.AddMarshalInfo ((PEAPI.NativeType) yyVals[-2+yyTop]);

                                yyVal = param_def;
			  }

void case_413()
#line 2040 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = new ArrayList ();
                                /* type_list.Add (TypeRef.Ellipsis);*/
				type_list.Add (new SentinelTypeRef ());
                                yyVal = type_list;
                          }

void case_414()
#line 2047 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = (ArrayList) yyVals[-2+yyTop];
                                /* type_list.Add (TypeRef.Ellipsis);*/
				type_list.Add (new SentinelTypeRef ());
				yyVal = type_list;
                          }

void case_415()
#line 2054 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = new ArrayList ();
                                type_list.Add (yyVals[-1+yyTop]);
                                yyVal = type_list;
                          }

void case_416()
#line 2060 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = (ArrayList) yyVals[-4+yyTop];
                                type_list.Add (yyVals[-1+yyTop]);
                          }

void case_421()
#line 2075 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
							codegen.CurrentMethodDef.AddInstr (new
                                        EmitByteInstr ((int) yyVals[0+yyTop], tokenizer.Location));
                          
						}

void case_423()
#line 2085 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] != null) {
                                        codegen.CurrentMethodDef.AddLocals (
                                                (ArrayList) yyVals[-1+yyTop]);
                                }
                          }

void case_424()
#line 2092 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] != null) {
                                        codegen.CurrentMethodDef.AddLocals (
                                                (ArrayList) yyVals[-1+yyTop]);
                                        codegen.CurrentMethodDef.InitLocals ();
                                }
                          }

void case_425()
#line 2100 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.EntryPoint ();
                                codegen.HasEntryPoint = true;
                          }

void case_430()
#line 2112 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef,
                                        (BaseTypeRef) yyVals[-2+yyTop], (string) yyVals[0+yyTop]);
                                
                          }

void case_431()
#line 2118 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef.Signature,
					(BaseMethodRef) yyVals[0+yyTop]);
                          }

void case_432()
#line 2125 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-10+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef methref;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                if (owner.UseTypeSpec) {
                                        methref = new TypeSpecMethodRef (owner, (CallConv) yyVals[-12+yyTop], (BaseTypeRef) yyVals[-11+yyTop],
                                                (string) yyVals[-8+yyTop], param_list, (int) yyVals[-5+yyTop]);
                                } else {
                                        methref = owner.GetMethodRef ((BaseTypeRef) yyVals[-11+yyTop],
                                                (CallConv) yyVals[-12+yyTop], (string) yyVals[-8+yyTop], param_list, (int) yyVals[-5+yyTop]);
                                }

				codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef.Signature,
					methref);
			  }

void case_434()
#line 2149 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int index = (int) yyVals[-2+yyTop];
                                ParamDef param = codegen.CurrentMethodDef.GetParam (index);
                                codegen.CurrentCustomAttrTarget = param;

                                if (param == null) {
                                        Report.Warning (tokenizer.Location, String.Format ("invalid param index ({0}) with .param", index));
                                } else if (yyVals[0+yyTop] != null)
                                        param.AddDefaultValue ((Constant) yyVals[0+yyTop]);
                          }

void case_442()
#line 2173 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_445()
#line 2182 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList local_list = new ArrayList ();
                                local_list.Add (yyVals[0+yyTop]);
                                yyVal = local_list;
                          }

void case_446()
#line 2188 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList local_list = (ArrayList) yyVals[-2+yyTop];
                                local_list.Add (yyVals[0+yyTop]);
                          }

void case_452()
#line 2219 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* This is a reference to a global method in another*/
                                /* assembly. This is not supported in the MS version of ilasm*/
                          }

void case_453()
#line 2224 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                string module = (string) yyVals[-1+yyTop];

                                if (codegen.IsThisModule (module)) {
                                    /* This is not handled yet.*/
                                } else {
                                    yyVal = codegen.ExternTable.GetModuleTypeRef ((string) yyVals[-1+yyTop], "<Module>", false);
                                }

                          }

void case_455()
#line 2238 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = new HandlerBlock ((LabelInfo) yyVals[-2+yyTop],
                                        codegen.CurrentMethodDef.AddLabel ());
                                codegen.CurrentMethodDef.EndLocalsScope ();
                          }

void case_456()
#line 2246 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = codegen.CurrentMethodDef.AddLabel ();
                                codegen.CurrentMethodDef.BeginLocalsScope ();
                          }

void case_457()
#line 2254 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                TryBlock try_block = (TryBlock) yyVals[-1+yyTop];

                                ArrayList clause_list = (ArrayList) yyVals[0+yyTop];
                                foreach (object clause in clause_list)
                                        try_block.AddSehClause ((ISehClause) clause);

                                codegen.CurrentMethodDef.AddInstr (try_block);
                          }

void case_459()
#line 2270 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
				
                                yyVal = new TryBlock (new HandlerBlock (from, to), tokenizer.Location);
                          }

void case_460()
#line 2277 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
				
				yyVal = new TryBlock (new HandlerBlock (from, to), tokenizer.Location);
			  }

void case_461()
#line 2286 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList clause_list = new ArrayList ();
                                clause_list.Add (yyVals[0+yyTop]);
                                yyVal = clause_list;
                          }

void case_462()
#line 2292 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList clause_list = (ArrayList) yyVals[-1+yyTop];
                                clause_list.Add (yyVals[0+yyTop]);
                          }

void case_463()
#line 2299 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				if (yyVals[-1+yyTop].GetType () == typeof (PrimitiveTypeRef))
					Report.Error ("Exception not be of a primitive type.");
					
                                BaseTypeRef type = (BaseTypeRef) yyVals[-1+yyTop];
                                CatchBlock cb = new CatchBlock (type);
                                cb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = cb;
                          }

void case_464()
#line 2309 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                FinallyBlock fb = new FinallyBlock ();
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = fb;
                          }

void case_465()
#line 2315 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                FaultBlock fb = new FaultBlock ();
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = fb;
                          }

void case_466()
#line 2321 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                FilterBlock fb = (FilterBlock) yyVals[-1+yyTop];
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                          }

void case_467()
#line 2328 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                HandlerBlock block = (HandlerBlock) yyVals[0+yyTop];
                                FilterBlock fb = new FilterBlock (block);
                                yyVal = fb;
                          }

void case_468()
#line 2334 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
                                FilterBlock fb = new FilterBlock (new HandlerBlock (from, null));
                                yyVal = fb;
                          }

void case_469()
#line 2340 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
				FilterBlock fb = new FilterBlock (new HandlerBlock (from, null));
				yyVal = fb;
			  }

void case_471()
#line 2352 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{	
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);

                                yyVal = new HandlerBlock (from, to);
                          }

void case_472()
#line 2359 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);

				yyVal = new HandlerBlock (from, to);
			  }

void case_473()
#line 2368 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new SimpInstr ((Op) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_474()
#line 2373 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));        
                          }

void case_475()
#line 2378 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int slot = codegen.CurrentMethodDef.GetNamedLocalSlot ((string) yyVals[0+yyTop]);
                                if (slot < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], slot, tokenizer.Location));
                          }

void case_476()
#line 2386 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_477()
#line 2391 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int pos = codegen.CurrentMethodDef.GetNamedParamPos ((string) yyVals[0+yyTop]);
                                if (pos < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));

                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], pos, tokenizer.Location));
                          }

void case_478()
#line 2400 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new
                                        IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_479()
#line 2405 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int slot = codegen.CurrentMethodDef.GetNamedLocalSlot ((string) yyVals[0+yyTop]);
                                if (slot < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));
                                codegen.CurrentMethodDef.AddInstr (new
                                        IntInstr ((IntOp) yyVals[-1+yyTop], slot, tokenizer.Location));
                          }

void case_480()
#line 2413 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] is MiscInstr) {
                                        switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_i8:
                                        codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop],
                                                (long) yyVals[0+yyTop], tokenizer.Location));
                                        break;
                                        }
                                }
                          }

void case_481()
#line 2424 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                case MiscInstr.ldc_r4:
                                case MiscInstr.ldc_r8:
                                         codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], (double) yyVals[0+yyTop], tokenizer.Location));
                                         break;
                                }
                          }

void case_482()
#line 2433 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[0+yyTop];
                                
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_r4:
                                        case MiscInstr.ldc_r8:
                                        codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], (double) l, tokenizer.Location));
                                        break;
                                }
                          }

void case_483()
#line 2444 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				byte[] fpdata;
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_r4:
						fpdata = (byte []) yyVals[0+yyTop];
						if (!BitConverter.IsLittleEndian) {
							System.Array.Reverse (fpdata, 0, 4);
						}
                                                float s = BitConverter.ToSingle (fpdata, 0);
                                                codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], s, tokenizer.Location));
                                                break;
                                        case MiscInstr.ldc_r8:
						fpdata = (byte []) yyVals[0+yyTop];
						if (!BitConverter.IsLittleEndian) {
							System.Array.Reverse (fpdata, 0, 8);
						}
                                                double d = BitConverter.ToDouble (fpdata, 0);
                                                codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], d, tokenizer.Location));
                                                break;
                                }
                          }

void case_484()
#line 2466 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo target = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
                                codegen.CurrentMethodDef.AddInstr (new BranchInstr ((BranchOp) yyVals[-1+yyTop],
								   target, tokenizer.Location));  
                          }

void case_485()
#line 2472 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo target = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
                                codegen.CurrentMethodDef.AddInstr (new BranchInstr ((BranchOp) yyVals[-1+yyTop],
                                        			   target, tokenizer.Location));
                          }

void case_486()
#line 2478 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new MethodInstr ((MethodOp) yyVals[-1+yyTop],
                                        (BaseMethodRef) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_487()
#line 2483 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-2+yyTop];
                                GenericParamRef gpr = yyVals[-3+yyTop] as GenericParamRef;
                                if (gpr != null && codegen.CurrentMethodDef != null)
                                        codegen.CurrentMethodDef.ResolveGenParam ((PEAPI.GenParam) gpr.PeapiType);
                                IFieldRef fieldref = owner.GetFieldRef (
                                        (BaseTypeRef) yyVals[-3+yyTop], (string) yyVals[0+yyTop]);

                                codegen.CurrentMethodDef.AddInstr (new FieldInstr ((FieldOp) yyVals[-4+yyTop], fieldref, tokenizer.Location));
                          }

void case_488()
#line 2495 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                GlobalFieldRef fieldref = codegen.GetGlobalFieldRef ((BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);

                                codegen.CurrentMethodDef.AddInstr (new FieldInstr ((FieldOp) yyVals[-2+yyTop], fieldref, tokenizer.Location));
                          }

void case_489()
#line 2501 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new TypeInstr ((TypeOp) yyVals[-1+yyTop],
                                        (BaseTypeRef) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_490()
#line 2506 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if ((MiscInstr) yyVals[-1+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr ((string) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_491()
#line 2511 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] bs = (byte[]) yyVals[0+yyTop];
                                if ((MiscInstr) yyVals[-3+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr (bs, tokenizer.Location));
                          }

void case_492()
#line 2517 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] bs = (byte[]) yyVals[0+yyTop];
                                if ((MiscInstr) yyVals[-2+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr (bs, tokenizer.Location));
                          }

void case_493()
#line 2523 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] arg_array = null;

                                if (arg_list != null)
                                        arg_array = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));

                                codegen.CurrentMethodDef.AddInstr (new CalliInstr ((CallConv) yyVals[-4+yyTop],
                                        (BaseTypeRef) yyVals[-3+yyTop], arg_array, tokenizer.Location));
                          }

void case_494()
#line 2534 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if ((MiscInstr) yyVals[-1+yyTop] == MiscInstr.ldtoken) {
                                        if (yyVals[0+yyTop] is BaseMethodRef)
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((BaseMethodRef) yyVals[0+yyTop], tokenizer.Location));
                                        else if (yyVals[0+yyTop] is IFieldRef)
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((IFieldRef) yyVals[0+yyTop], tokenizer.Location));
                                        else
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((BaseTypeRef) yyVals[0+yyTop], tokenizer.Location));
                                                
                                }
                          }

void case_496()
#line 2553 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                GenericArguments ga = (GenericArguments) yyVals[-3+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

				BaseMethodRef methref = codegen.GetGlobalMethodRef ((BaseTypeRef) yyVals[-5+yyTop], (CallConv) yyVals[-6+yyTop],
                                        (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));

                                if (ga != null)
                                        methref = methref.GetGenericMethodRef (ga);

                                yyVal = methref;
                          }

void case_497()
#line 2573 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-6+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                GenericArguments ga = (GenericArguments) yyVals[-3+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef methref;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                if (codegen.IsThisAssembly ("mscorlib")) {
                                        PrimitiveTypeRef prim = owner as PrimitiveTypeRef;
                                        if (prim != null && prim.SigMod == "")
                                                owner = codegen.GetTypeRef (prim.Name);
                                }

                                if (owner.UseTypeSpec) {
                                        methref = new TypeSpecMethodRef (owner, (CallConv) yyVals[-8+yyTop], (BaseTypeRef) yyVals[-7+yyTop],
                                                (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));
                                } else {
                                        methref = owner.GetMethodRef ((BaseTypeRef) yyVals[-7+yyTop],
                                                (CallConv) yyVals[-8+yyTop], (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));
                                }

                                if (ga != null)
                                        methref = methref.GetGenericMethodRef (ga);
                                
                                yyVal = methref;
                          }

void case_499()
#line 2608 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = new ArrayList ();
                                label_list.Add (codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]));
                                yyVal = label_list;
                          }

void case_500()
#line 2614 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = new ArrayList ();
                                label_list.Add (yyVals[0+yyTop]);
                                yyVal = label_list;
                          }

void case_501()
#line 2620 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = (ArrayList) yyVals[-2+yyTop];
                                label_list.Add (codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]));
                          }

void case_502()
#line 2625 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = (ArrayList) yyVals[-2+yyTop];
                                label_list.Add (yyVals[0+yyTop]);
                          }

void case_506()
#line 2640 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-2+yyTop];

                                yyVal = owner.GetFieldRef (
                                        (BaseTypeRef) yyVals[-3+yyTop], (string) yyVals[0+yyTop]);
                          }

void case_509()
#line 2659 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                EventDef event_def = new EventDef ((FeatureAttr) yyVals[-2+yyTop],
                                        (BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
                                codegen.CurrentTypeDef.BeginEventDef (event_def);
                                codegen.CurrentCustomAttrTarget = event_def;
                          }

void case_516()
#line 2687 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddAddon (
                                        (MethodRef) yyVals[0+yyTop]);                                
                          }

void case_517()
#line 2692 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddRemoveon (
                                        (MethodRef) yyVals[0+yyTop]);
                          }

void case_518()
#line 2697 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddFire (
                                        (MethodRef) yyVals[0+yyTop]);
                          }

void case_519()
#line 2702 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddOther (
                                        (MethodRef) yyVals[0+yyTop]);
                          }

void case_520()
#line 2707 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_524()
#line 2722 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                PropertyDef prop_def = new PropertyDef ((FeatureAttr) yyVals[-6+yyTop], (BaseTypeRef) yyVals[-5+yyTop],
                                        (string) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop]);
                                codegen.CurrentTypeDef.BeginPropertyDef (prop_def);
                                codegen.CurrentCustomAttrTarget = prop_def;

                                if (yyVals[0+yyTop] != null) {
                                        prop_def.AddInitValue ((Constant) yyVals[0+yyTop]);
                                }
                          }

void case_534()
#line 2769 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                         }

void case_539()
#line 2783 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = new CustomAttr ((BaseMethodRef) yyVals[-2+yyTop],
                                        (byte[]) yyVals[0+yyTop]);
                          }

void case_543()
#line 2794 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-5+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                yyVal = owner.GetMethodRef ((BaseTypeRef) yyVals[-6+yyTop],
                                        (CallConv) yyVals[-7+yyTop], (string) yyVals[-3+yyTop], param_list, 0);
                          }

void case_544()
#line 2808 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                yyVal = codegen.GetGlobalMethodRef ((BaseTypeRef) yyVals[-4+yyTop], (CallConv) yyVals[-5+yyTop],
                                        (string) yyVals[-3+yyTop], param_list, 0);
                          }

void case_547()
#line 2831 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				System.Text.UnicodeEncoding ue = new System.Text.UnicodeEncoding ();
				PermissionSetAttribute psa = new PermissionSetAttribute ((System.Security.Permissions.SecurityAction) (short) yyVals[-2+yyTop]);
				psa.XML = ue.GetString ((byte []) yyVals[0+yyTop]);
				yyVal = new PermPair ((PEAPI.SecurityAction) yyVals[-2+yyTop], psa.CreatePermissionSet ());
			  }

void case_548()
#line 2838 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				PermissionSetAttribute psa = new PermissionSetAttribute ((System.Security.Permissions.SecurityAction) (short) yyVals[-1+yyTop]);
				psa.XML = (string) yyVals[0+yyTop];
				yyVal = new PermPair ((PEAPI.SecurityAction) yyVals[-1+yyTop], psa.CreatePermissionSet ());
			  }

void case_550()
#line 2850 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList list = new ArrayList ();
				list.Add (yyVals[0+yyTop]);
				yyVal = list;
			  }

void case_551()
#line 2856 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList list = (ArrayList) yyVals[-2+yyTop];
				list.Add (yyVals[0+yyTop]);
				yyVal = list;
			  }

void case_553()
#line 2870 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				  ArrayList list = new ArrayList ();
				  list.Add (yyVals[0+yyTop]);
				  yyVal = list;
			  }

void case_554()
#line 2876 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				  ArrayList list = (ArrayList) yyVals[-1+yyTop];
				  list.Add (yyVals[0+yyTop]);
				  yyVal = list;
			  }

void case_555()
#line 2884 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				NameValuePair pair = (NameValuePair) yyVals[0+yyTop];
				yyVal = new PermissionMember ((MemberTypes) yyVals[-2+yyTop], (BaseTypeRef) yyVals[-1+yyTop], pair.Name, pair.Value);
			  }

void case_556()
#line 2889 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				NameValuePair pair = (NameValuePair) yyVals[0+yyTop];
				yyVal = new PermissionMember ((MemberTypes) yyVals[-3+yyTop], (BaseTypeRef) yyVals[-1+yyTop], pair.Name, pair.Value);
			  }

void case_562()
#line 2920 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList pairs = new ArrayList ();
				pairs.Add (yyVals[0+yyTop]);
				yyVal = pairs;
			  }

void case_563()
#line 2926 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
			  	ArrayList pairs = (ArrayList) yyVals[-2+yyTop];
				pairs.Add (yyVals[0+yyTop]);
				yyVal = pairs;
			  }

void case_592()
#line 3046 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                /* We need to compute the hash ourselves. :-(*/
                                /* AssemblyName an = AssemblyName.GetName ((string) $3);*/
                          }

void case_597()
#line 3073 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				codegen.CurrentCustomAttrTarget = null;
				codegen.CurrentDeclSecurityTarget = null;
			  }

void case_598()
#line 3080 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.SetThisAssembly ((string) yyVals[0+yyTop], (PEAPI.AssemAttr) yyVals[-1+yyTop]);
                                codegen.CurrentCustomAttrTarget = codegen.ThisAssembly;
				codegen.CurrentDeclSecurityTarget = codegen.ThisAssembly;
                          }

void case_616()
#line 3142 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                System.Reflection.AssemblyName asmb_name = 
					new System.Reflection.AssemblyName ();
				asmb_name.Name = (string) yyVals[0+yyTop];
				codegen.BeginAssemblyRef ((string) yyVals[0+yyTop], asmb_name, (PEAPI.AssemAttr) yyVals[-1+yyTop]);
                          }

void case_617()
#line 3149 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                System.Reflection.AssemblyName asmb_name = 
					new System.Reflection.AssemblyName ();
				asmb_name.Name = (string) yyVals[-2+yyTop];
				codegen.BeginAssemblyRef ((string) yyVals[0+yyTop], asmb_name, (PEAPI.AssemAttr) yyVals[-3+yyTop]);
                          }

void case_626()
#line 3184 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_646()
#line 3228 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
				FileStream s = new FileStream ((string) yyVals[0+yyTop], FileMode.Open, FileAccess.Read);
				byte [] buff = new byte [s.Length];
				s.Read (buff, 0, (int) s.Length);
				s.Close ();

				codegen.AddManifestResource (new ManifestResource ((string) yyVals[0+yyTop], buff, (yyVals[-1+yyTop] == null) ? 0 : (uint) yyVals[-1+yyTop]));
			  }

void case_657()
#line 3257 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[0+yyTop];
                                byte[] intb = BitConverter.GetBytes (l);
                                yyVal = BitConverter.ToInt32 (intb, BitConverter.IsLittleEndian ? 0 : 4);
                          }

void case_660()
#line 3269 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-1+yyTop];
                                byte[] intb = BitConverter.GetBytes (i);
                                yyVal = (double) BitConverter.ToSingle (intb, 0);
                          }

void case_661()
#line 3275 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[-1+yyTop];
                                byte[] intb = BitConverter.GetBytes (l);
                                yyVal = (double) BitConverter.ToSingle (intb, BitConverter.IsLittleEndian ? 0 : 4);
                          }

void case_662()
#line 3281 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] intb = BitConverter.GetBytes ((long) yyVals[-1+yyTop]);
				yyVal = BitConverter.ToDouble (intb, 0);
                          }

void case_663()
#line 3286 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] intb = BitConverter.GetBytes ((int) yyVals[-1+yyTop]);
                                yyVal = (double) BitConverter.ToSingle (intb, 0);
                          }

void case_666()
#line 3300 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = yyVals[-1+yyTop];
                                tokenizer.InByteArray = false;
                          }

void case_668()
#line 3308 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = (ArrayList) yyVals[0+yyTop];
                                yyVal = byte_list.ToArray (typeof (byte));
                          }

void case_669()
#line 3315 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = new ArrayList ();
                                byte_list.Add (Convert.ToByte (yyVals[0+yyTop]));
                                yyVal = byte_list;
                          }

void case_670()
#line 3321 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = (ArrayList) yyVals[-1+yyTop];
                                byte_list.Add (Convert.ToByte (yyVals[0+yyTop]));
                          }

#line default
   static readonly short [] yyLhs  = {              -1,
    0,    1,    1,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,   19,   19,   19,   19,   20,   20,
   20,    8,   21,   21,   21,   21,   21,    4,   23,    3,
   25,   31,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   27,   29,   29,   30,
   30,   33,   33,   28,   28,   35,   35,   36,   36,   38,
   38,   39,   39,   32,   32,   32,   32,   32,   32,   34,
   34,   41,   41,   41,   41,   41,   41,   42,   43,   43,
   44,   44,   45,   45,   40,   40,   40,   26,   26,   46,
   46,   46,   46,   46,   46,   46,   46,   46,   46,   46,
   46,   53,   46,   46,   37,   37,   37,   37,   37,   37,
   37,   37,   37,   37,   37,   37,   37,   37,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   56,   55,   55,   57,   57,   57,   57,   57,   51,   51,
   51,   58,   58,   58,   58,   58,   58,   58,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,    6,   62,   62,   63,   63,   63,
   63,   63,   63,   63,   63,   63,   63,   63,   63,   63,
   63,   63,   64,   64,   65,   65,   67,   67,   67,   67,
   67,   67,   67,   67,   67,   67,   67,   67,   67,   67,
   67,   66,   66,   66,   66,    7,   71,   71,   73,   73,
   72,   72,   74,   74,   75,   75,   75,   75,   75,   75,
   75,   75,   75,   75,   75,   75,   75,   75,   75,   75,
   75,    5,   76,   76,   78,   78,   78,   78,   78,   78,
   78,   78,   78,   78,   78,   78,   78,   78,   78,   78,
   78,   78,   78,   78,   78,   78,   78,   81,   81,   81,
   81,   81,   81,   81,   81,   81,   81,   81,   81,   81,
   81,   81,   50,   50,   50,   79,   79,   79,   79,   80,
   80,   80,   80,   80,   80,   80,   80,   80,   80,   80,
   80,   80,   54,   54,   82,   82,   83,   83,   83,   83,
   83,   52,   52,   52,   52,   52,   84,   84,   77,   77,
   85,   85,   85,   85,   85,   85,   85,   85,   85,   85,
   85,   85,   85,   85,   85,   85,   85,   85,   85,   85,
   85,   85,   85,   86,   86,   86,   91,   91,   91,   91,
   92,   49,   49,   49,   88,   93,   89,   94,   94,   94,
   95,   95,   96,   96,   96,   96,   98,   98,   98,   97,
   97,   97,   90,   90,   90,   90,   90,   90,   90,   90,
   90,   90,   90,   90,   90,   90,   90,   90,   90,   90,
   90,   90,   90,   90,   90,   87,   87,  100,  100,  100,
  100,  100,   99,   99,  101,  101,  101,   47,  102,  102,
  104,  104,  104,  103,  103,  105,  105,  105,  105,  105,
  105,  105,   48,  106,  108,  108,  108,  108,  107,  107,
  109,  109,  109,  109,  109,  109,   16,   16,   16,   16,
   16,   16,  110,  110,   15,   15,   15,   15,   15,  113,
  113,  114,  115,  115,  116,  116,  118,  118,  118,  117,
  117,  112,  112,  119,  120,  120,  120,  120,  120,  120,
  120,  120,  111,  111,  111,  111,  111,  111,  111,  111,
  111,  111,  111,  111,  111,  111,  111,   14,   14,   14,
    9,    9,  121,  121,  122,  122,   10,  123,  125,  125,
  124,  124,  126,  126,  126,  126,  126,  126,  126,  127,
  127,  127,  127,  127,   11,  128,  128,  129,  129,  130,
  130,  130,  130,  130,  130,  130,   12,  131,  133,  133,
  133,  133,  133,  133,  133,  133,  133,  133,  132,  132,
  134,  134,  134,  134,   13,  135,  137,  137,  137,  136,
  136,  138,  138,  138,   60,   60,   17,   18,   68,   68,
   68,   68,   68,  139,  141,   70,  140,  140,  142,  142,
   69,   69,   22,   22,   24,   24,   24,
  };
   static readonly short [] yyLen = {           2,
    1,    0,    2,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    2,    2,    3,
    2,    2,    1,    1,    3,    2,    5,    4,    2,    4,
    6,    7,    0,    2,    2,    2,    2,    4,    2,    4,
    6,    2,    0,    2,    2,    3,    3,    3,    3,    3,
    3,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    0,    2,    0,
    1,    2,    3,    0,    3,    0,    3,    1,    3,    0,
    3,    1,    3,    1,    3,    2,    3,    2,    3,    3,
    5,    0,    2,    2,    2,    2,    2,    1,    3,    5,
    1,    3,    1,    3,    4,    5,    1,    0,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    2,
    2,    0,   15,    1,    1,    1,    3,    6,    3,    3,
    4,    2,    2,    2,    5,    5,    7,    1,    1,    1,
    1,    1,    1,    1,    2,    1,    2,    1,    2,    1,
    2,    1,    2,    3,    2,    1,    1,    1,    1,    1,
    1,    1,    3,    0,    1,    1,    3,    2,    2,    2,
    1,    0,    1,    1,    2,    2,    2,    2,    0,    6,
    5,    5,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    2,    1,    2,    1,    2,    1,
    2,    1,    2,    3,    4,    6,    5,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    2,    4,
    1,    2,    2,    1,    2,    1,    2,    1,    2,    1,
    0,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    2,    2,    2,    2,    1,    3,    2,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    2,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    7,    0,    3,    0,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    5,
    2,    2,    0,    2,    0,    2,    4,    4,    4,    4,
    4,    4,    4,    4,    4,    4,    4,    4,    4,    4,
    4,    1,    2,    1,    1,    2,    4,    2,    0,    1,
    3,    1,    1,    3,    5,    5,    4,    3,    2,    5,
    5,    5,    5,    5,    5,    2,    2,    2,    2,    2,
    2,    4,   11,   14,    0,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    8,    6,    5,    0,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    4,    4,
    4,    4,    1,    1,    1,    0,    4,    4,    4,    0,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    0,    1,    1,    3,    2,    3,    1,    6,
    7,    0,    1,    3,    3,    5,    0,    1,    0,    2,
    2,    2,    4,    5,    1,    1,    4,    6,    4,    4,
    3,   15,    1,    5,    1,    2,    1,    1,    1,    1,
    1,    1,    1,    0,    1,    3,    1,    2,    2,    3,
    3,    3,    4,    1,    3,    1,    2,    2,    4,    4,
    1,    2,    3,    2,    2,    2,    2,    2,    2,    1,
    4,    4,    1,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    5,    3,    2,    2,
    4,    3,    6,    2,    4,    7,    9,    0,    1,    1,
    3,    3,    1,    1,    2,    5,    3,    4,    4,    3,
    0,    2,    2,    0,    2,    2,    2,    2,    2,    1,
    1,    1,    4,    8,    0,    2,    2,    2,    0,    2,
    2,    2,    2,    1,    1,    1,    2,    4,    4,    5,
    7,    7,    8,    6,    6,    3,    4,    3,    6,    1,
    3,    5,    1,    2,    3,    4,    3,    4,    6,    1,
    1,    1,    3,    3,    1,    1,    4,    1,    6,    6,
    6,    4,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    2,    3,
    8,    4,    0,    2,    0,    1,    4,    3,    0,    2,
    0,    2,    3,    8,    2,    3,    3,    1,    1,    3,
    8,    2,    3,    1,    4,    4,    6,    0,    2,    8,
    3,    3,    2,    3,    3,    1,    4,    4,    0,    2,
    2,    3,    3,    3,    3,    3,    3,    1,    0,    2,
    2,    3,    1,    3,    4,    3,    0,    2,    2,    0,
    2,    4,    3,    1,    1,    3,    1,    1,    1,    4,
    4,    4,    4,    1,    0,    4,    0,    1,    1,    2,
    1,    1,    1,    1,    1,    3,    1,
  };
   static readonly short [] yyDefRed = {            2,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  345,    0,  647,    0,    0,    0,    0,    0,
    0,    3,    4,    5,    6,    7,    8,    9,   10,   11,
   12,   13,   14,   15,   16,   17,   23,   24,    0,    0,
    0,    0,    0,    0,    0,    0,  599,    0,    0,    0,
  658,   21,  657,   19,    0,    0,    0,  173,  174,    0,
    0,  171,    0,  320,    0,    0,  278,    0,    0,    0,
    0,    0,  673,  674,  677,    0,  675,    0,    0,    0,
  573,  574,  575,  576,  577,  578,  579,  580,  581,  582,
  583,  584,  585,  586,  587,    0,    0,   22,   18,    0,
    2,  108,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  316,  322,  419,  601,  618,  639,  650,
    0,  600,    0,    0,  638,    0,   59,   44,   45,   67,
   66,   52,   54,   55,   56,   57,   58,   60,   61,   62,
   63,   64,    0,   65,   53,    0,    0,    0,    0,    0,
  159,  160,  139,  140,  141,  142,  143,  144,    0,  146,
  148,  150,  152,    0,    0,  156,  158,  157,    0,    0,
  126,  161,  125,    0,   84,    0,  503,  138,    0,  504,
  169,  170,  175,  176,  177,  178,    0,    0,    0,    0,
    0,    0,    0,   20,  594,    0,    0,   25,    0,  346,
  347,  348,  349,  361,  362,  360,  350,  351,  352,  353,
  356,  354,  355,  357,  358,  364,    0,  363,  359,  386,
    0,    0,  648,  649,    0,    0,  655,    0,    0,    0,
    0,    0,    0,  323,    0,    0,  341,    0,  340,    0,
  339,    0,  338,    0,  336,    0,  337,    0,    0,  665,
    0,  329,    0,    0,    0,    0,    0,    0,    0,  631,
  630,    0,    0,   47,   46,   48,   49,   50,   51,   92,
    0,    0,    0,    0,   86,   88,    0,    0,  155,  153,
  145,  147,  149,  151,    0,  505,    0,    0,    0,    0,
    0,  133,  132,  134,    0,    0,    0,    0,    0,    0,
    0,  384,  383,    0,    0,    0,    0,  539,  317,  277,
    0,  286,  279,  280,  281,  287,  288,  289,  282,  283,
  284,  285,  291,  292,    0,  596,    0,    0,    0,    0,
    0,    0,    0,    0,  547,    0,   33,   38,   40,   43,
  511,    0,    0,    0,  525,    0,  111,  110,  114,  115,
  116,  118,  117,  124,  119,  109,  112,  113,    0,    0,
  321,    0,    0,    0,    0,    0,    0,  659,    0,    0,
    0,    0,    0,    0,    0,  328,  456,  342,  473,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  425,    0,    0,    0,    0,    0,    0,
    0,  426,  443,  439,  442,  440,  441,    0,  435,  420,
  433,  437,  438,  419,    0,  597,    0,    0,    0,    0,
  609,  608,  602,  615,    0,    0,    0,    0,    0,  626,
  619,  627,    0,    0,    0,  643,  640,  645,    0,    0,
  654,  651,    0,    0,  632,  633,  634,  635,  636,  637,
    0,    0,    0,    0,    0,    0,   87,   89,  127,  154,
    0,    0,   85,    0,    0,    0,  129,  130,  165,    0,
    0,  162,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   27,    0,    0,    0,    0,    0,
    0,  562,    0,    0,  550,  656,    0,    0,    0,  121,
    0,    0,  120,  514,  529,  324,  327,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  664,  669,    0,
    0,  478,  479,  480,  482,  481,  483,  484,  485,    0,
  486,    0,  489,    0,    0,    0,  494,    0,  474,  475,
  476,  477,  421,    0,    0,    0,  422,    0,    0,    0,
    0,    0,  458,    0,  436,    0,    0,    0,    0,    0,
    0,  461,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   92,   75,    0,   93,   94,   95,   97,   96,    0,   69,
    0,   41,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  131,    0,    0,    0,    0,    0,    0,
    0,  413,    0,    0,    0,  218,    0,    0,    0,    0,
    0,  184,  185,  186,  187,  188,  189,  190,  191,  192,
  193,  194,    0,  196,  198,  200,  202,  208,  209,  210,
  211,  212,  213,  214,  215,  216,  217,    0,  221,  224,
  226,  230,  228,    0,    0,    0,    0,   31,    0,    0,
  367,  375,  376,  377,  378,  370,  371,  372,    0,  369,
  373,  374,    0,    0,    0,    0,    0,    0,    0,  545,
    0,    0,  549,    0,    0,   34,   35,   36,   37,  512,
  513,    0,    0,    0,    0,   99,  528,  526,  527,    0,
    0,    0,  335,  334,  333,  332,    0,    0,    0,    0,
  330,  331,  326,  325,  666,  670,    0,    0,    0,    0,
  492,    0,  500,  499,    0,    0,    0,    0,    0,  445,
    0,    0,    0,  431,    0,    0,    0,    0,    0,  455,
    0,  469,  468,  467,    0,  470,  464,  465,  462,  466,
  607,  606,  603,    0,  625,  624,  621,  622,    0,    0,
    0,    0,    0,    0,   82,    0,   98,   90,   72,    0,
    0,    0,    0,    0,    0,   77,  506,    0,  167,  163,
  135,  136,    0,  542,    0,  544,    0,    0,  229,  225,
  223,    0,    0,    0,  227,  195,  197,  199,  201,  222,
  247,  233,  234,  235,  236,  237,  238,  239,  240,  241,
  242,  261,    0,  251,  252,  253,  254,  255,  256,  257,
  258,  259,  232,  262,  263,  264,  265,  266,  267,  268,
  269,  270,  271,  272,  273,  274,    0,    0,  290,  203,
  294,    0,  275,    0,    0,  366,    0,    0,  387,  388,
  389,    0,    0,  671,  672,    0,    0,    0,  565,  564,
  563,    0,  551,   32,    0,    0,    0,    0,  508,    0,
    0,    0,    0,  520,  521,  522,  515,  523,    0,    0,
    0,  534,  535,  536,  530,  660,  661,  663,  662,    0,
  491,    0,  495,    0,    0,    0,  448,  423,    0,    0,
    0,    0,  430,    0,  460,  459,  429,  463,    0,    0,
    0,    0,  652,    0,   81,    0,   73,  409,    0,    0,
    0,  405,    0,    0,    0,  128,    0,  414,    0,  418,
  415,    0,    0,    0,  243,  244,  245,  246,  260,    0,
    0,  250,  249,  204,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  315,    0,  296,  312,  591,    0,  379,  380,  381,  382,
    0,    0,    0,    0,  561,  560,    0,  553,    0,    0,
  100,    0,  516,  518,  519,  517,  532,  533,  531,  487,
    0,  502,  501,    0,  451,  446,  450,  424,    0,  434,
    0,    0,    0,    0,   91,   83,  137,    0,    0,    0,
    0,  543,    0,    0,    0,    0,  248,    0,    0,  205,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  313,  365,    0,    0,    0,    0,
    0,    0,    0,  552,  554,    0,    0,    0,    0,  493,
  428,    0,  472,  471,    0,    0,    0,  408,  406,    0,
  496,  416,    0,  181,  182,  207,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  390,  567,    0,    0,    0,  572,    0,
    0,  555,    0,    0,    0,    0,    0,    0,    0,  180,
  206,  311,  309,  305,  303,  301,  299,  297,  300,  298,
  310,  306,  304,  302,  308,  307,    0,    0,    0,    0,
    0,  556,    0,    0,  524,    0,  604,  620,    0,  497,
    0,  396,  391,  393,  392,  394,  395,  397,  399,  400,
  401,  402,  398,  569,  570,  571,    0,    0,  557,    0,
    0,    0,  390,  558,    0,    0,    0,  411,    0,    0,
    0,    0,  559,    0,    0,    0,    0,    0,    0,  123,
  432,
  };
  protected static readonly short [] yyDgoto  = {             1,
    2,   22,   23,   24,   25,   26,   27,   28,   29,   30,
   31,   32,   33,   34,   35,   36,  470,   52,   37,   38,
  497,   77,   39,  123,   40,  232,   50,  271,  454,  592,
    0,  173,  593,  451, 1010,  599,  174,  589,  766,  175,
  452,  768,  409,    0,  176,  356,  357,  358,  177,  598,
  530,  613, 1164,  919,  471,  178,  472,   62,  654,  490,
  837,   67,  193,  656,  843,  963,  964,  371,  859,  252,
   41,  114,   65,  233,  115,   42,  253,   72,  614, 1118,
  487,  921,  922,  931,  410,  729,  286,  746,  412,  413,
  730,  731,  414,  415,  561,  562,  747,  563,  179,  725,
  180,  359,  701,  498,  877,  360,  702,  502,  885,   63,
   96,  491,  494,  495,  977,  978,  979, 1092,  492,  860,
   69,  327,   43,  254,   48,  423,    0,   44,  255,  431,
   45,  256,  126,  437,   46,  257,   79,  442,  519,  520,
  375,  521,
  };
  protected static readonly short [] yySindex = {            0,
    0, 6988, -174,  -99, -154, -105,   71, -329,  -45, -193,
   83, -105,    0,  -89,    0,  257, 1265, 1265, -154, -105,
   99,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  133,  145,
  328,  171,  176,  187,  192,  196,    0, -184, -137, 5163,
    0,    0,    0,    0, 5420,  522,  522,    0,    0,  204,
 2119,    0,  233,    0,  241, -105,    0, -105, -234,  248,
 -109, 2530,    0,    0,    0,  257,    0,  269,  -81,  269,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 5492, -163,    0,    0, -105,
    0,    0,  820,  271,  258,  312,  325,  370,  376,  393,
  268,  275,  -26,    0,    0,    0,    0,    0,    0,    0,
 -184,    0,  269,  278,    0,  181,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  633,    0,    0, -117,  -66,  -50,  105, -268,
    0,    0,    0,    0,    0,    0,    0,    0,  769,    0,
    0,    0,    0,  522,  468,    0,    0,    0, 2119,  583,
    0,    0,    0,  487,    0,  278,    0,    0,  298,    0,
    0,    0,    0,    0,    0,    0,  121,  522, 4856,   26,
  348,  345, 4823,    0,    0, -202,  362,    0, -105,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  365,    0,    0,    0,
  269,  257,    0,    0,  269,  382,    0, -136,  384,  399,
 6952, 5174, -189,    0,  241, -105,    0, -105,    0, -105,
    0, -154,    0, -205,    0, -205,    0,  419,  442,    0,
  449,    0, 6473,  398,  254,  210,  663, -243,  257,    0,
    0,  866,  269,    0,    0,    0,    0,    0,    0,    0,
  307,  257,   49,  486,    0,    0,  468,  259,    0,    0,
    0,    0,    0,    0, 2119,    0,  445, 1055,  257,  102,
   39,    0,    0,    0,  467,  469,  522,  257,   51, 2119,
  994,    0,    0,  269,  484,  471,  384,    0,    0,    0,
  481,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  177,    0,  436,  537,  553,  566,
 5551,  269,  566,  468,    0,  592,    0,    0,    0,    0,
    0, 5492, -105,  332,    0, -105,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  586,  598,
    0,  820,  599,  608,  613,  615,  668,    0,  602,  672,
  683,  688,  566,  566,  640,    0,    0,    0,    0,  486,
 -154,  -52,  486,  522, 2119, 5492, -236,  522, 5420,  705,
  486,  486, -105,    0,  708, -240, -105, 5629, -223,  568,
 -105,    0,    0,    0,    0,    0,    0,  706,    0,    0,
    0,    0,    0,    0,  731,    0,  444, -138,  715, -105,
    0,    0,    0,    0,  718, -112,  721,  722, -105,    0,
    0,    0,  609,  625,  257,    0,    0,    0,  627,  257,
    0,    0,  257,  269,    0,    0,    0,    0,    0,    0,
 -191, -167, -158,  591,   90,  257,    0,    0,    0,    0,
 5135, 2119,    0,    0,  741,   93,    0,    0,    0,  733,
    2,    0,  468,  468,  742,  108,  257,  528,  226,  735,
 6253,  666,  744,  763,    0, -227, -148,  -73,  377,   44,
   32,    0,  759,  140,    0,    0, -292, 5211,  777,    0,
  324, 5344,    0,    0,    0,    0,    0,  -45,  -45,  -45,
  -45,  207,  220,  -45,  -45,  -19,  116,    0,    0,  766,
  640,    0,    0,    0,    0,    0,    0,    0,    0, 2119,
    0, 1055,    0,  -22,  384, 2119,    0,  486,    0,    0,
    0,    0,    0, -105, 5699,  783,    0,  522,  782, -105,
  547,  551,    0,  794,    0, 6560, 2119,  568, -238, -238,
  731,    0, -238, -105,  449,  384,  449,  796,  449,  449,
  384,  449,  449,  799,  257,  257,  269,  257, -197,  278,
    0,    0, -158,    0,    0,    0,    0,    0,  241,    0,
 -158,    0,  803,  257,  278,  808,  810,  445, -125,  487,
  241,  257, -105,    0, -164,  811,  817,   26,  257,  269,
  821,    0,  223, 5551,  614,    0,  628,  624,  827,  261,
  649,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, -297,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 6187,    0,    0,
    0,    0,    0,  137,  241,  823,  449,    0,  566, 1644,
    0,    0,    0,    0,    0,    0,    0,    0,  828,    0,
    0,    0,  829,  837,  842,  843,  853,  838, -188,    0,
  566,  862,    0,  468,  241,    0,    0,    0,    0,    0,
    0,    0,  257,  226, -105,    0,    0,    0,    0, -153,
  673,  337,    0,    0,    0,    0,  861,  867,  878,  881,
    0,    0,    0,    0,    0,    0, 4856,    0,  859,  449,
    0,  432,    0,    0,  358,  886,   33,  177,  451,    0,
 2119, 5699, 2119,    0,  226,  887, -105,  241, -105,    0,
  -83,    0,    0,    0,  486,    0,    0,    0,    0,    0,
    0,    0,    0, -105,    0,    0,    0,    0, -105,  269,
  269,  278, -105, -167,    0,  489,    0,    0,    0, -158,
  278,  875,  226,  889, 2119,    0,    0,  102,    0,    0,
    0,    0,  384,    0,  735,    0,  877,  177,    0,    0,
    0,  566,  895,  896,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   97,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -130,  -64,    0,    0,
    0,  776,    0,  850,  384,    0,  229,  232,    0,    0,
    0, 6253,  907,    0,    0,  908,  910,  384,    0,    0,
    0,  274,    0,    0,  269,  674,  917,  154,    0,  522,
  522,  522,  522,    0,    0,    0,    0,    0,  522,  522,
  522,    0,    0,    0,    0,    0,    0,    0,    0,  241,
    0,  735,    0,  486,  819,  924,    0,    0, 5699,  177,
  531, 5135,    0,  823,    0,    0,    0,    0,  684,  690,
  921,  931,    0,  241,    0, -158,    0,    0,  934, 5551,
  938,    0,  445,  735,  487,    0,  574,    0, 5551,    0,
    0,   65, -105, -105,    0,    0,    0,    0,    0,  937,
  566,    0,    0,    0, -105,  -56,  943,  953,  954,  962,
  963,  969,  970,  971,  977,  978,  979,  980,  985,  449,
    0,  384,    0,    0,    0, 1910,    0,    0,    0,    0,
  161,  875, -105,  -96,    0,    0, -230,    0, 5274,  522,
    0,  735,    0,    0,    0,    0,    0,    0,    0,    0,
  577,    0,    0,  241,    0,    0,    0,    0,  986,    0,
 -105,  241, -105, -105,    0,    0,    0,  276,  875,  991,
  589,    0,  177,  566,  993, 1000,    0,  384, 1007,    0,
 -105,  247, -154, -154, -154, -154, -126, -126, -154, -154,
 -154, -154, -154, -154,    0,    0,  226, 1011, 1012, 1005,
 1013, 1014, 1017,    0,    0,  468, 1026, 2119,  595,    0,
    0,  226,    0,    0, 1016, 1018, 1020,    0,    0,  735,
    0,    0,  120,    0,    0,    0, 1023, 1022, 1033, 1042,
 1046, 1047, 1048, 1051, 1058, 1060, 1061, 1064, 1067, 1069,
 1070, 1071, 1024,    0,    0, -105, -105, -105,    0, 1026,
 1043,    0, 1055,  823, 1057, -105, -105, 6253,  603,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  875,  458, 1073, 1080,
 1083,    0, 1078, 1081,    0, 5758,    0,    0,  186,    0,
 1085,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  449, 1089,    0,  226,
   33,  241,    0,    0, 1101,  735, 1092,    0,  458, 1091,
 1093, 1079,    0, 1097, 1098,  875,  735, 1099,  610,    0,
    0,
  };
  protected static readonly short [] yyRindex = {            0,
    0, 1373, -175, 5984,    0,    0, 5836,  318, 4985, -203,
    0,    0,    0,  743,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  214,    0,
    0,    0,    0,    0,    0, 5836, 5836,    0,    0,    0,
    0,    0, 2971,    0,  403,    0,    0,    0,    0, 3531,
 3732, 5836,    0,    0,    0,    0,    0, 2517,    0, 1109,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 4387, 4387, 4387, 4387, 4387, 4387,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1114, 1110,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, -219,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 5836,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 3419,    0, 1646,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 5836,    0,    0,
    0,    0,    0,    0,    0,  477,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 2782,    0,    0,    0, 1112, 4160,    0,    0, 4275,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1121,    0,    0,
    0,    0, 1123,    0,    0,    0,    0,    0,    0,    0,
 -217,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 2443,    0,    0, 2708,
 1118,    0,    0,    0,    0,    0, 5836,    0,    0,    0,
 1118,    0,    0,  413,    0,    0, 3083,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 4561, 3643, 3840, 2260,
    0,  848,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1126,    0,    0,    0,    0,    0,
    0,    0,    0, 5836,    0,    0,    0, 5836,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1380,    0,    0,    0,    0,    0,    0,
    0,  456,    0, 1136,    0, 3945,    0,    0,    0,    0,
    0,    0,    0, 6148,    0,    0,    0,    0,    0,  260,
    0,    0,    0,    0, 3195,    0,    0,    0,    0, 1321,
  344,  299,    0,    0,    0, 2260,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 4387, 4387, 4387,
 4387,    0,    0, 4387, 4387,    0,    0,    0,    0,    0,
 1133,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 6647,    0,    0,  634,    0,    0,
    0,    0,    0,    0,  645,    0,    0, 5836,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 6734,    0,    0,    0,    0,  975,    0,    0,    0,    0,
  653,    0,    0,    0,    0,    0,  426,    0,    0, 1144,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1149, 4048, 1912, 5196,    0, 1148,    0,   -4,
    0,    0,  296,    0,  330,    0,    0,    0,    0, 4702,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  430,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -139,    0,    0,
    0,    0,    0,    0,    0,  601,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1151,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  -40,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 6256,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  652,    0,    0,
    0,  645,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  745,
 1049,  746,    0,  456,    0,    0,    0,    0,    0,    0,
 2178, 1853,    0,    0,    0,    0,    0, 2708,    0,    0,
    0,    0, 3307,    0, 1321,    0, 5906,  544,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  -97,    0,    0,    0,
    0,    0,    0, 4606, 2260,    0,    0,    0,    0,    0,
    0,  344,    0,    0,    0,  665,    0,  676,    0,    0,
    0,    0,    0,    0, 1156,    0,    0,    0,    0, 5836,
 5836, 5836, 5836,    0,    0,    0,    0,    0, 5836, 5836,
 5836,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 1321,    0,    0, 6821,    0,    0,    0,    0,  685,
    0,    0,    0, 6908,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1153,    0, 1148, 1321,   13,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 4499,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 1853,    0,    0,    0,    0,    0,    0,    0, 5836,
    0, 1321,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  693, 5906,    0,
    0,    0,  544,    0,    0,    0,    0,  438,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1321,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1159, 1148,    0,    0,  344,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1853, 1160,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  698,    0,    0,    0, 1587,    0,    0, 1163,    0,
 1169,    0,    0,    0,    0, 1853, 1321,    0,    0,    0,
    0,
  };
  protected static readonly short [] yyGindex = {            0,
 1330,    0, 1212,    0, 1213, 1215, -185,    0,    0,    0,
    0,    0,    0,    0, -220, -212,   -6,   17, -221, -192,
    0,  -30,    0,  -13,    0,    0,    0,  772,    0,    0,
    0, -431,    0,    0, -271,    0,  -57,  689,    0, -160,
  880,  542, 1230,    0,  -41,    0,    0,    0,  -88, -171,
   -5, -760,    0, -919,    0,  492,  863,    0, -837,  -67,
    0,  -17,    0,    0, -839,    0,  349, -237,  455, -141,
    0,    0,    0,    0,  -75,    0, 1059,    0, -207,  327,
 -476,    0,  475,  473,    0,  756, -367, -232,    0,    0,
  590,    0,    0,    0,    0,  929, -465,    0, 1102,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1195,
 1475,    0,    0,  812,    0,  517,    0,  405,  816,    0,
    0,  654,    0,    0, 1452,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  981,    0,
    0,    0,
  };
  protected static readonly short [] yyTable = {            54,
   78,   61,   80,  189,  287,   71,  124,  226,  372,  660,
  353,  351,  331,   99,  971,  463,  531,  306,  467,  352,
  411,  590,  227,   73,  927,   74,   75,  234,  377,  229,
  545,  406,  404,  421,  191,   98,  146, 1044,  259,  354,
  405,  422,  430,  436,  441,  550,  350,   74,  308,   68,
  181,  182, 1038,  336,  593,  196,  593,  593,  368,  192,
  407,  194,  221,  222, 1000,  225,  220,  403,  222,   73,
  227,   74,   75,   73,   53,   74,   75,  685,  361,  258,
  187,  581,  599,  362,  599,  599,  335,  237,  239,  241,
  243,  245,  247,  230,  748,  227,  582,  750,   53,   73,
  305,   74,   75,  583,   73,  273,   74,   75,   51,  376,
  187,  288,  263,  584,  228,  291,  459,  276,  585,  469,
  227,  326,  307,  661,  148,  292,  293,  443,  290,  231,
  334,  991,  231,  231,  250,  325,   51,  368,  940,  565,
  231,  275,  941,  659,  526,  299,  227,  775,  222,  942,
  198,  765,  586,  796,  797,  798,  799,   53,  285,  769,
  686,  687,  776, 1011,  199,  570,   53,  553,   73,  270,
   74,   75,  763,  493,  219,  304,   73,  800,   74,   75,
  734,  219,  300,  377,   73,  291,   74,   75,   64,  278,
  279,   73,  329,   74,   75,  292,  293, 1131,   53,  465,
   74,   74,   47,   68,  363,  944,  280,   73,  332,   74,
   51,  368,   53, 1020,  688,  689,  945,  675,  250,  675,
  675, 1049,  408,   66, 1021,  675,  510,  461,  675,  364,
  455,  365,  274,  366,  662,  663,  664,  665,  675,  675,
  527,  675,  478,  458,  250,  444, 1168,  466,  250,  369,
  370,  251,  713,  499, 1125,  720,  476,  464,  367,  273,
 1129,  336,  486,  666,  667,  668,  669,  457,   78,  298,
  546,  604,  534,  489,  605,  908,  272,   49,  745,  670,
  501,  671,  672,   78,  227,   79,  506,   76,  975,  976,
   73,   61,   74,   75,  482,   53,  250,  533,  293, 1099,
   79,   53,  195,  680,  681,  516,  517,  611,  468,  549,
  223,  224,  606,  607,  674,  675,  676,  678,  456,  535,
  477,  679,  469,  411,  336,  744,  774,  532,  369,  370,
  259,  587,  259,  593,  406,  404,  500, 1014,  917,  503,
  165,   55,   70,  405,  231,  336,  294,  295,  296,  523,
  566,   68,  529,  943, 1040, 1041, 1042,  122,  571,  594,
  540,  542,  602,  407,  854,  855,  599,  100,  966,  552,
  403,  259,  597,  522,  259,  298,  528,  609,   73,  588,
   74,   75,  536,  259,  539,  541,  543,  714,  462,  259,
  547, 1100,  721,  551,  554, 1161,  336,  524,  525,  101,
  336,  580,  369,  370,  600,  838, 1169,  683,  839,  693,
  673,  102,  684,  568,  595,  840,  294,  295,  296,  222,
  125,  577,  574,  752,  982,  753,  579,  755,  756,  838,
  757,  758, 1037,  745,   73,  595,   74,  116,   73,  840,
   74,   75,  117,  719,  700,  291,  299,  304,   56,   57,
   58,   59,   60,  118,  838,  292,  293, 1152,  119,  675,
  675,  675,  120,  298,  840,  610,  784,  692,  707,  708,
  696,  629,  717,  629,  629,  610,  595,  432,  722,  875,
  883,  709,  710,   73, 1006,   74,   75,  728,  874,  882,
  703,  704,  705,  706,  786,  787,  711,  712,   73,  741,
   74,  718,  983,  984,  985,  986,  926,  724,  876,  884,
  190,  987,  988,  989,   73,  844,   74,   75,  857,  433,
  197,  424,  866,  493,  434,  408,   66,  743,  236,  166,
    7,  723,  166,   73,  222,   74,  762,  726,  435,  302,
  783,  235,  733,  736,  291,  303,  248,  935,  936,  937,
  938,  742,  771,  249,  292,  293,  788,  751,  767,  259,
  778,  760,  761,  903,  920,  168,  293,  771,  168,  297,
  777,  939,  260,  261,    7,  319,  293,  319,  891,  929,
   66,   73,  238,   74,  319,  425,  183,  184,  185,  186,
  426,  845,  695,   66,  103,  240,  779,  319,  262,  164,
  295,  923,  164,  277,  878,  629,  629,  104,  293,  427,
  428,  858,  179,  293,  310,  179,  293,  293,  429,  293,
  293,  328,  179,  293,  841,  309,  293,  293,  597,  893,
  894,  629,  293,  293,   73,  330,   74,   75,   66,  293,
  242,  293,  293,  293,   66,  291,  244,  293,  293,  293,
  293,  293,  333,  293,  864,  292,  293,    7,  293,  293,
  293,   66, 1057,  246,  336,  416,  293,  879,  337,  318,
   11,   12,  856,  900,  728,  902,  294,  295,  296,  865,
  610,  103,  318,  385,  880,  299,  868,  103,  867,  373,
  302,  103,  103,  641,  103,  881,  303,  897,  183,  385,
  291,  183,  892,  304,  793,  794,  220,  906,  183,  220,
  292,  293,  374,   80,  910,   80,  220,  925,    7,  250,
  896,  610,  898,  899,  932,   73,  453,   74,   75,  417,
  905,  462,  907,  460,  418,  641,  187,  473,  909,  474,
  641,  480,  588,   73,  595,   74,  641,  911,   53,   17,
   18,  481,  912,  419,  641,  291,  913,  930,  479,  610,
  915,  916,  420,  677,  920,  292,  293,  483,  319,  319,
  319,  319,  319,  319,  962,  294,  295,  296,  105,  106,
  107,  108,  109,  110,  967,  968,  595,  969,  970, 1074,
 1076,  595,  975,  976,  595,  595,  291,  595,  595,  854,
  855,  920,  998,  899,  595,  595,  596,  293,  595,  484,
  595,  595,  485,  999,  417,  417,  417,  595, 1035,  595,
  595,  595,  319,  319,  227,   73,  319,   74,  595,  595,
   53,  946,  111,  112,  377,  501,  113,  595,  595, 1132,
   73,  728,   74,   75,  595, 1012,  787,  676, 1050,  787,
  496,  289,  504,  318,  318,  318,  318,  318,  318,  990,
 1061,  787, 1008,  993,  505, 1083, 1094,  787,  295,  997,
  507, 1013,  512, 1018, 1130,  787,  294,  295,  296,  508,
 1095, 1171,  787,  767,  509, 1090,  510,  992,  304, 1133,
 1134, 1135, 1136, 1137, 1138, 1139, 1140, 1141, 1142,   56,
   57,   58,   59,   60,  518,  498,  498,  318,  318,  920,
  295,  318,  103,  103,  103,  295,  444,  444,  295,  295,
  623,  295,  295,  447,  447,  295, 1015, 1016,  295,  295,
  438,  294,  295,  296,  295,  295,  566,  566, 1019,  511,
  869,  295,  513,  295,  295,  295, 1063,  568,  568,  295,
  295,  295,  295,  295,  514,  295,  449,  449,  920,  515,
  295,  295,  295, 1051,  407,  407, 1039, 1043,  295,  410,
  410, 1054,  439,  623, 1048,  538,  544, 1058, 1156,  555,
  870,  564,  930,    7,  623,  575,  294,  295,  296,  623,
 1093,  440,  567,    7, 1053,  569, 1055, 1056,  572,  573,
 1143,  576,  871,  578, 1124, 1154,   11,   12,  623,  623,
  588,  591,  644,  653, 1067,  601,  603,  623,  612,  608,
  872,  657,  658,  610,  264,  265,  266,  294,  295,  296,
  873,  267,  268,  269,  227,  655,  682,  715,  610, 1069,
 1070, 1071, 1072, 1073, 1075, 1077, 1078, 1079, 1080, 1081,
 1082,  694,  588,  732,  644,  653,  735,  588,  737,  644,
  588,  588,  738,  588,  588,  644,  653,  739,  600,  754,
  588,  588,  759,  644,  653,  770,  588,  588,  772, 1119,
 1120, 1121,  781,  588,  773,  588,  588,  588,  782, 1127,
 1128,  785,  789,  790,  588,  588,  791,  792,  795,  104,
  842,  847,  848,  588,  588,  676,  849,  676,  676,  299,
  588,  850,  851,  103,  676,  676,  676,  676,  676,  676,
  676, 1158,  676,  852,  270,  676,  676,  676,  862,  676,
  676,  676,  886,  890,  676,  676,  610,  676,  887,  676,
  676,  676,  676,  676, 1157,  676,  676,  676,  676,  888,
  676,  676,  889,  676,  676,  895,  904,  676,  918,  924,
  928,  676,  676,  933,  934,  676,  676,  676,  676,  676,
  676,  676,  676,  326,  676,  676,  676,  972,  973,  676,
  974,  676,  676,  980,  676,  676,  981,  676,  676,  994,
  676,  676,  676,  995, 1003, 1001,  676,  676,  676,  676,
  676, 1002,  676,  676, 1004, 1007, 1017,  676,  676,  676,
 1009,  676,  676, 1022,  676,  676,  676,  676,  676,  281,
  282,  283,  284, 1023, 1024,  947,  948,  949,  950,  951,
  952,  953, 1025, 1026,  676,  954,  955,  956,  957, 1027,
 1028, 1029,  605,  557,  558,  559,  560, 1030, 1031, 1032,
 1033,   73,  676,   74,   75, 1034,   53,  445,  446,  447,
 1052, 1060, 1064,  468,  448,  449,  450,  676,  676, 1065,
  105,  106,  107,  108,  109,  110, 1066,  469, 1086,  676,
  958,  959, 1084, 1085,  960, 1091, 1087, 1088, 1089, 1096,
 1098, 1097, 1101, 1102, 1117,  605,  676,  676,  676,  676,
  676,  676,  676,  676, 1103,  676,  605,  676,  676,  676,
  676,  605,   73, 1104,   74,   75,  642, 1105, 1106, 1107,
 1123,  961, 1108,  301,  111,  112,  605,  605,  113, 1109,
  605, 1110, 1111,  292,  293, 1112,  272,  148, 1113,  605,
 1114, 1115, 1116, 1126, 1144,  676,  676,  676,  676,  676,
  676, 1145,  676,  676, 1146, 1150, 1153,  676,  642, 1155,
 1160, 1162, 1163,  642,  676,  787, 1165, 1166, 1167,  642,
 1170,  103,    1,  103,  103,   39,  598,  642,  646,  104,
  103,  103,  103,  103,  103,  103,  103,  616,  103,  628,
  164,  103,  103,  103,  676,  103,  103,  667,  676,  676,
  103,  103,   70,  103,  668,  103,  103,  103,  103,  103,
  617,  103,  103,  103,  103,   71,  103,  103,   76,  103,
  103,   74,  509,  103,  404,  295,  343,  103,  103,  344,
  231,  103,  103,  103,  103,  103,  103,  103,  103,  122,
  103,  103,  103,  347,  348,  103,  349,  103,  103,  853,
  103,  103,  914,  103,  103, 1005,  103,  103,  103,  149,
  764,  355,  103,  103,  103,  103,  103,  780,  103,  103,
 1047, 1149,  556,  103,  103,  103, 1068,  103,  103, 1159,
  103,  103,  103, 1059,  103, 1062,  150,  901,  996,  749,
  537,  475,   97, 1045, 1122,  863,  861,  965,  121,    0,
  103,  716,    0,  151,  152,  153,  154,  155,  156,  157,
  158,    0,  159,    0,  160,  161,  162,  163,  103,    0,
    0,    0,    0,    0,    0,    0,    0,  947,  948,  949,
  950,  951,  952,  953,  103,    0,    0,  954,  955,  956,
  957,    0,    0,    0,    0,  103,    0,    0,    0,    0,
    0,    0,  188,  165,  294,  295,  296,  166,    0,  167,
  168,    0,  103,  103,  103,  103,  103,  103,  103,  103,
    0,  103,    0,  103,  103,  103,  103,    0,  386,    0,
  386,  386,  958,  959,    0,    0, 1147,    0,    0,  386,
    0,    0,  412,  412,    0,    0,    0,    0,    0,    0,
    0,  170,    0,  386,    0,  171,  172,    0,    0,    0,
    0,  103,  103,  103,  103,  103,  103,    0,  103,  103,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1148,
  103,    0,    0,    0,    0,    0,    0,  104,    0,  104,
  104,    0,    0,    0,    0,  107,  104,  104,  104,  104,
  104,  104,  104,    0,  104,    0,    0,  104,  104,  104,
  103,  104,  104,    0,  103,  103,  104,  104,    0,  104,
    0,  104,  104,  104,  104,  104,    0,  104,  104,  104,
  104,    0,  104,  104,    0,  104,  104,    0,    0,  104,
    0,    0,    0,  104,  104,    0,    0,  104,  104,  104,
  104,  104,  104,  104,  104,    0,  104,  104,  104,    0,
    0,  104,    0,  104,  104,    0,  104,  104,    0,  104,
  104,    0,  104,  104,  104,  386,    0,    0,  104,  104,
  104,  104,  104,    0,  104,  104,    0,    0,    0,  104,
  104,  104,    0,  104,  104,    0,  104,  104,  104,    0,
  104,    0,  386,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  104,    0,    0,  386,
  386,  386,  386,  386,  386,  386,  386,    0,  386,    0,
  386,  386,  386,  386,  104,   81,   82,   83,   84,   85,
   86,   87,   88,   89,   90,   91,   92,   93,   94,   95,
  104,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  104,    0,    0,    0,    0,    0,    0,  386,  386,
    0,    0,    0,  386,    0,  386,  386,    0,  104,  104,
  104,  104,  104,  104,  104,  104,    0,  104,    0,  104,
  104,  104,  104,    0,  386,    0,  386,  386,    0,    0,
    0,    0,    0,    0,    0,  386,    0,  412,    0,  412,
    0,    0,    0,    0,    0,    0,    0,  386,    0,  386,
    0,  386,  386,    0,    0,    0,    0,  104,  104,  104,
  104,  104,  104,    0,  104,  104,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  104,    0,    0,    0,
    0,    0,    0,  107,    0,  107,  107,    0,    0,    0,
    0,  105,  107,  107,  107,  846,  107,  107,  107,    0,
  107,    0,    0,  107,  107,  107,  104,    0,  107,    0,
  104,  104,  107,  107,    0,  107,    0,  107,  107,  107,
  107,  107,    0,  107,  107,  107,  107,    0,  107,  107,
    0,  107,  107,    0,    0,  107,    0,    0,    0,  107,
  107,    0,    0,  107,  107,  107,  107,  107,  107,  107,
  107,    0,  107,  107,  107,    0,    0,  107,    0,  107,
  107,    0,  107,  107,    0,  107,  107,    0,  107,  107,
  107,  386,    0,    0,  107,  107,  107,  107,  107,    0,
  107,  107,    0,    0,    0,  107,  107,  107,    0,  107,
  107,    0,  107,  107,  107,    0,    0,    0,  386,    0,
    0,    0,    0,    0,    0,    0,  662,  663,  664,  665,
    0,    0,  107,    0,    0,  386,  386,  386,  386,  386,
  386,  386,  386,    0,  386,    0,  386,  386,  386,  386,
  107,    0,    0,    0,    0,  666,  667,  668,  669,    0,
    0,    0,    0,    0,    0,    0,  107,    0,    0,    0,
    0,  670,    0,  671,  672,    0,    0,  107,    0,    0,
    0,    0,    0,    0,  386,  386,    0,    0,    0,  386,
    0,  386,  386,    0,  107,  107,  107,  107,  107,  107,
  107,  107,    0,  107,    0,  107,  107,  107,  107,    0,
  386,    0,  386,  386,    0,    0,    0,    0,    0,    0,
    0,  386,    0,    0,  403,    0,    0,    0,    0,    0,
    0,    0,    0,  386,    0,  386,    0,  386,  386,    0,
    0,    0,    0,  107,  107,  107,  107,  107,  107,    0,
  107,  107,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  107,    0,    0,    0,    0,    0,    0,  105,
    0,  105,  105,    0,    0,    0,    0,  106,  105,  105,
  105, 1036,  105,  105,  105,    0,  105,    0,    0,  105,
  105,  105,  107,    0,  105,    0,  107,  107,  105,  105,
    0,  105,  673,  105,  105,  105,  105,  105,    0,  105,
  105,  105,  105,    0,  105,  105,    0,  105,  105,    0,
    0,  105,    0,    0,    0,  105,  105,    0,    0,  105,
  105,  105,  105,  105,  105,  105,  105,    0,  105,  105,
  105,    0,    0,  105,    0,  105,  105,    0,  105,  105,
    0,  105,  105,    0,  105,  105,  105,  386,    0,    0,
  105,  105,  105,  105,  105,    0,  105,  105,    0,    0,
    0,  105,  105,  105,    0,  105,  105,    0,  105,  105,
  105,    0,    0,    0,  386,    0,    0,    0,    0,    0,
    0,    0,  662,  663,  664,  665,    0,    0,  105,    0,
    0,  386,  386,  386,  386,  386,  386,  386,  386,    0,
  386,    0,  386,  386,  386,  386,  105,    0,    0,    0,
    0,  666,  667,  668,  669,    0,    0,    0,    0,    0,
    0,    0,  105,    0,    0,    0,    0,  670,    0,  671,
  672,    0,    0,  105,    0,    0,    0,    0,    0,    0,
  386,  386,    0,    0,    0,  386,    0,  386,  386,    0,
  105,  105,  105,  105,  105,  105,  105,  105,    0,  105,
    0,  105,  105,  105,  105,    0,   73,    0,   74,   75,
    0,    0,    0,    0,    0,    0,    0,  187,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  386,
    0,  148,    0,  386,  386,    0,    0,    0,    0,  105,
  105,  105,  105,  105,  105,    0,  105,  105,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  105,    0,
    0,    0,    0,    0,    0,  106,    0,  106,  106,    0,
    0,    0,   76,    0,  106,  106,  106,    0,  106,  106,
  106,    0,  106,    0,    0,  106,  106,  106,  105,    0,
  106,    0,  105,  105,  106,  106,    0,  106,  673,  106,
  106,  106,  106,  106,    0,  106,  106,  106,  106,    0,
  106,  106,    0,  106,  106,    0,    0,  106,    0,    0,
    0,  106,  106,    0,    0,  106,  106,  106,  106,  106,
  106,  106,  106,    0,  106,  106,  106,    0,    0,  106,
    0,  106,  106,    0,  106,  106,  589,  106,  106,    0,
  106,  106,  106,  149,    0,    0,  106,  106,  106,  106,
  106,  368,  106,  106,    0,    0,    0,  106,  106,  106,
    0,  106,  106,    0,  106,  106,  106,    0,    0,    0,
  150,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  106,    0,    0,  151,  152,  153,
  154,  155,  156,  157,  158,    0,  159,    0,  160,  161,
  162,  163,  106,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  106,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  106,
    0,    0,    0,    0,    0,    0,  188,  165,    0,    0,
    0,  166,    0,  167,  168,    0,  106,  106,  106,  106,
  106,  106,  106,  106,    0,  106,    0,  106,  106,  106,
  106,    0,  368,  368,  368,  368,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  170,    0,    0,    0,  171,
  172,  368,  368,  368,  368,  106,  106,  106,  106,  106,
  106,    0,  106,  106,    0,    0,    0,  368,    0,  368,
  368,    0,    0,    0,  106,    0,    0,    0,    0,    0,
   76,    0,   76,   76,    0,    0,    0,   76,    0,   76,
   76,   76,    0,   76,   76,   76,    0,   76,    0,    0,
    0,   76,   76,    0,  106,   76,    0,    0,  106,  106,
   76,    0,   76,    0,   76,   76,   76,   76,   76,    0,
   76,   76,   76,   76,    0,   76,   76,    0,   76,   76,
    0,    0,   76,    0,    0,    0,   76,   76,    0,    0,
   76,   76,   76,   76,   76,   76,   76,   76,    0,   76,
   76,   76,    0,    0,   76,    0,   76,   76,    0,   76,
   76,  590,   76,   76,  589,   76,   76,   76,    0,    0,
    0,   76,   76,   76,   76,   76,    0,   76,   76,    0,
    0,    0,   76,   76,   76,    0,   76,   76,    0,   76,
   76,   76,    0,    0,    0,    0,    0,    0,  368,    0,
    0,    0,    0,    0,    0,    0,  589,    0,    0,   76,
    0,  589,    0,    0,  589,  589,    0,  589,  589,    0,
    0,    0,    0,    0,  589,  589,    0,   76,    0,    0,
  589,  589,    0,    0,    0,    0,    0,  589,    0,  589,
  589,  589,    0,   76,    0,    0,    0,    0,  589,  589,
    0,    0,    0,    0,   76,    0,    0,  589,  589,    0,
    0,    0,    0,    0,  589,    0,    0,    0,    0,    0,
    0,   76,   76,   76,   76,   76,   76,   76,   76,    0,
   76,    0,   76,   76,   76,   76,    0,   56,   57,   58,
   59,   60,    0,    0,    0,    0,    0,    0,    0,    0,
  200,  201,  202,  203,    0,  204,  205,  206,  207,  208,
  209,  210,    0,    0,    0,    0,    0,    0,  211,    0,
   76,   76,   76,   76,   76,   76,    0,   76,   76,    0,
    0,  212,  213,  214,  215,  216,  217,    0,    0,   76,
    0,    0,    0,    0,    0,   76,    0,   76,   76,    0,
  537,    0,    0,    0,   76,   76,   76,    0,   76,   76,
   76,    0,   76,    0,    0,    0,   76,   76,    0,   76,
   76,    0,    0,   76,   76,   76,    0,   76,    0,   76,
   76,   76,   76,   76,    0,   76,   76,   76,   76,    0,
   76,   76,    0,   76,   76,    0,    0,   76,    0,    0,
    0,   76,   76,    0,    0,   76,   76,   76,   76,   76,
   76,   76,   76,    0,   76,   76,   76,    0,    0,   76,
    0,   76,   76,    0,   76,   76,    0,   76,   76,  590,
   76,   76,   76,    0,    0,    0,   76,   76,   76,   76,
   76,    0,   76,   76,    0,    0,    0,   76,   76,   76,
    0,   76,   76,    0,   76,   76,   76,  218,  219,    0,
    0,    0,  538,    0,    0,    0,    0,    0,    0,    0,
    0,  590,    0,    0,   76,    0,  590,    0,    0,  590,
  590,    0,  590,  590,    0,    0,    0,    0,    0,  590,
  590,    0,   76,    0,    0,  590,  590,    0,    0,    0,
    0,    0,  590,    0,  590,  590,  590,    0,    0,    0,
    0,    0,    0,  590,  590,    0,    0,    0,    0,   76,
    0,    0,  590,  590,    0,    0,    0,    0,    0,  590,
    0,    0,    0,    0,    0,    0,   76,   76,   76,   76,
   76,   76,   76,   76,    0,   76,    0,   76,   76,   76,
   76,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  540,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   76,   76,   76,   76,   76,
   76,    0,   76,   76,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   76,    0,    0,    0,  537,    0,
  537,    0,    0,    0,    0,    0,    0,  537,  537,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   76,    0,    0,    0,   76,   76,
  537,    0,  537,  537,  537,  537,  537,    0,  537,  537,
  537,  537,    0,  537,  537,    0,  537,  537,  537,    0,
  537,    0,    0,    0,    0,  537,    0,    0,  537,  537,
    0,  537,  537,  537,  537,  537,    0,  537,  537,  537,
  537,  537,  537,    0,  537,  537,  541,  537,  537,    0,
  537,  537,    0,  537,  537,  537,    0,    0,  537,  537,
  537,  537,  537,  537,    0,  537,  537,  537,  537,  537,
  537,  537,  537,    0,  537,  537,    0,  537,  537,  537,
  538,    0,  538,    0,    0,    0,    0,    0,    0,  538,
  538,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  538,    0,  538,  538,  538,  538,  538,    0,
  538,  538,  538,  538,    0,  538,  538,    0,  538,  538,
  538,    0,  538,    0,    0,    0,    0,  538,    0,    0,
  538,  538,    0,  538,  538,  538,  538,  538,    0,  538,
  538,  538,  538,  538,  538,    0,  538,  538,  454,  538,
  538,    0,  538,  538,    0,  538,  538,  538,    0,    0,
  538,  538,  538,  538,  538,  538,    0,  538,  538,  538,
  538,  538,  538,  538,  538,    0,  538,  538,    0,  538,
  538,  538,  540,    0,  540,    0,    0,    0,    0,    0,
    0,  540,  540,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  540,    0,  540,  540,  540,  540,
  540,    0,  540,  540,  540,  540,    0,  540,  540,    0,
  540,  540,  540,    0,  540,    0,    0,    0,    0,  540,
    0,    0,  540,  540,    0,  540,  540,  540,  540,  540,
    0,  540,  540,  540,  540,  540,  540,    0,  540,  540,
   29,  540,  540,    0,  540,  540,    0,  540,  540,  540,
    0,    0,  540,  540,  540,  540,  540,  540,    0,  540,
  540,  540,  540,  540,  540,  540,  540,    0,  540,  540,
    0,  540,  540,  540,  541,    0,  541,    0,    0,    0,
    0,    0,    0,  541,  541,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  541,    0,  541,  541,
  541,  541,  541,    0,  541,  541,  541,  541,    0,  541,
  541,    0,  541,  541,  541,    0,  541,    0,    0,    0,
    0,  541,    0,    0,  541,  541,    0,  541,  541,  541,
  541,  541,    0,  541,  541,  541,  541,  541,  541,    0,
  541,  541,   30,  541,  541,    0,  541,  541,    0,  541,
  541,  541,    0,    0,  541,  541,  541,  541,  541,  541,
    0,  541,  541,  541,  541,  541,  541,  541,  541,    0,
  541,  541,    0,  541,  541,  541,  454,    0,  454,  454,
    0,    0,    0,    0,    0,  454,  454,    0,    0,  454,
  454,    0,    0,  454,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  454,    0,
  454,  454,  454,  454,  454,    0,  454,  454,  454,  454,
    0,  454,  454,    0,  454,  454,    0,    0,  454,    0,
    0,   26,    0,  454,    0,    0,  454,  454,    0,  454,
  454,  454,  454,  454,    0,  454,  454,  454,    0,    0,
  454,    0,  454,  454,    0,  454,  454,    0,  454,  454,
    0,  454,  454,  454,    0,    0,    0,  454,  454,  454,
  454,  454,    0,  454,  454,    0,    0,    0,  454,  454,
  454,    0,  454,  454,    0,  454,  454,  454,   29,    0,
   29,    0,    0,    0,    0,    0,    0,   29,   29,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   29,    0,   29,   29,   29,   29,   29,    0,   29,   29,
   29,   29,    0,   29,   29,    0,   29,   29,   29,   28,
   29,    0,    0,    0,    0,   29,    0,    0,   29,   29,
    0,   29,   29,   29,   29,   29,    0,   29,   29,   29,
   29,   29,    0,    0,   29,   29,    0,    0,   29,    0,
   29,   29,    0,   29,   29,   29,    0,    0,   29,   29,
   29,   29,   29,   29,    0,   29,    0,    0,   29,   29,
   29,   29,   29,    0,   29,    0,    0,   29,   29,   29,
   30,    0,   30,    0,    0,    0,    0,    0,    0,   30,
   30,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   30,    0,   30,   30,   30,   30,   30,    0,
   30,   30,   30,   30,  452,   30,   30,    0,   30,   30,
   30,    0,   30,    0,    0,    0,    0,   30,    0,    0,
   30,   30,    0,   30,   30,   30,   30,   30,    0,   30,
   30,   30,   30,   30,    0,    0,   30,   30,    0,    0,
   30,    0,   30,   30,    0,   30,   30,   30,    0,   26,
   30,   30,   30,   30,   30,   30,    0,   30,   26,   26,
   30,   30,   30,   30,   30,    0,   30,    0,    0,   30,
   30,   30,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   26,    0,   26,   26,   26,   26,   26,    0,   26,
   26,   26,   26,    0,   26,   26,    0,   26,   26,   26,
    0,   26,    0,    0,    0,    0,   26,  453,    0,   26,
   26,    0,   26,   26,   26,   26,   26,    0,   26,   26,
   26,   26,   26,    0,    0,   26,   26,    0,    0,   26,
    0,   26,   26,    0,   26,   26,   26,    0,    0,   26,
   26,   26,   26,   26,   26,    0,   26,    0,    0,   26,
   26,   26,   26,   26,    0,   26,    0,   28,   26,   26,
   26,    0,    0,    0,    0,    0,   28,   28,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   28,
    0,   28,   28,   28,   28,   28,    0,   28,   28,   28,
   28,    0,   28,   28,    0,   28,   28,   28,    0,   28,
    0,    0,    0,    0,   28,    0,    0,   28,   28,  546,
   28,   28,   28,   28,   28,    0,   28,   28,   28,   28,
   28,    0,    0,   28,   28,    0,    0,   28,    0,   28,
   28,    0,   28,   28,   28,    0,    0,   28,   28,   28,
   28,   28,   28,    0,   28,    0,    0,   28,   28,   28,
   28,   28,    0,   28,    0,    0,   28,   28,   28,    0,
    0,  452,  452,    0,    0,  452,  452,    0,    0,  452,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  452,    0,  452,  452,  452,  452,
  452,    0,  452,  452,  452,  452,    0,  452,  452,    0,
  452,  452,    0,    0,  452,    0,    0,    0,    0,  452,
    0,    0,  452,  452,    0,  452,  452,  452,  452,  452,
    0,  452,  452,  452,  548,    0,  452,    0,  452,  452,
    0,  452,  452,    0,  452,  452,    0,  452,  452,  452,
    0,    0,    0,  452,  452,  452,  452,  452,    0,  452,
  452,    0,    0,    0,  452,  452,  452,    0,  452,  452,
    0,  452,  452,  452,  453,  453,    0,    0,  453,  453,
    0,    0,  453,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  453,    0,  453,
  453,  453,  453,  453,    0,  453,  453,  453,  453,    0,
  453,  453,    0,  453,  453,    0,    0,  453,    0,    0,
    0,    0,  453,    0,    0,  453,  453,    0,  453,  453,
  453,  453,  453,    0,  453,  453,  453,    0,    0,  453,
    0,  453,  453,    0,  453,  453,  276,  453,  453,    0,
  453,  453,  453,    0,    0,    0,  453,  453,  453,  453,
  453,    0,  453,  453,    0,    0,    0,  453,  453,  453,
    0,  453,  453,    0,  453,  453,  453,  546,    0,  546,
    0,    0,    0,    0,    0,    0,  546,  546,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  546,
    0,  546,  546,  546,  546,  546,    0,  546,  546,  546,
  546,    0,  546,  546,    0,  546,  546,    0,    0,  546,
    0,    0,    0,    0,  546,    0,    0,  546,  546,    0,
  546,  546,  546,  546,  546,    0,  546,  546,  546,    0,
    0,  546,    0,  546,  546,    0,  546,  546,  314,  546,
  546,    0,  546,  546,  546,    0,    0,    0,  546,  546,
  546,  546,  546,    0,  546,  546,    0,    0,    0,  546,
  546,  546,    0,  546,  546,    0,  546,  546,  546,    0,
    0,    0,  548,    0,  548,    0,    0,    0,    0,    0,
    0,  548,  548,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  592,    0,    0,    0,  548,    0,  548,  548,  548,  548,
  548,    0,  548,  548,  548,  548,    0,  548,  548,    0,
  548,  548,    0,    0,  548,    0,    0,    0,    0,  548,
    0,    0,  548,  548,    0,  548,  548,  548,  548,  548,
    0,  548,  548,  548,    0,  595,  548,    0,  548,  548,
    0,  548,  548,    0,  548,  548,    0,  548,  548,  548,
    0,    0,    0,  548,  548,  548,  548,  548,    0,  548,
  548,    0,    0,    0,  548,  548,  548,    0,  548,  548,
    0,  548,  548,  548,  276,    0,  276,    0,    0,    0,
    0,    0,    0,  276,  276,    0,    0,    0,    0,  276,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  276,    0,  276,  276,
  276,  276,  276,    0,  276,  276,  276,  276,    0,  276,
  276,    0,  276,  276,    0,    0,  276,    0,    0,    0,
    0,  276,    0,    0,  276,  276,    0,  276,  276,  276,
  276,  276,    0,  276,  276,  276,    0,    0,    0,    0,
  276,  276,    0,    0,  276,    0,  276,  276,    0,  276,
  276,  276,    0,    0,    0,  276,  276,  276,  276,  276,
    0,  276,    0,    0,    0,    0,  276,  276,  276,    0,
  276,    0,    0,  276,  276,  276,  314,    0,  314,    0,
    0,    0,    0,    0,    0,  314,  314,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  314,    0,
  314,  314,  314,  314,  314,    0,  314,  314,  314,  314,
    0,  314,  314,    0,  314,  314,    0,    0,  314,    0,
    0,    0,    0,  314,    0,    0,  314,  314,    0,  314,
  314,  314,  314,  314,    0,  314,  314,  314,  592,    0,
    0,    0,  314,  314,    0,    0,  314,    0,  314,  314,
    0,  314,  314,  314,    0,    0,    0,  314,  314,  314,
  314,  314,    0,  314,    0,    0,    0,    0,  314,  314,
  314,    0,  314,    0,    0,  314,  314,  314,    0,    0,
  592,    0,    0,  595,    0,  592,    0,    0,  592,  592,
    0,  592,  592,    0,    0,    0,    0,    0,  592,  592,
    0,    0,    0,    0,  592,  592,    0,    0,    0,    0,
    0,  592,    0,  592,  592,  592,    0,    0,    0,    0,
    0,    0,  592,  592,    0,  595,    0,    0,    0,    0,
  595,  592,  592,  595,  595,    0,  595,  595,  592,    0,
    0,    0,    0,  595,  595,    0,    0,    0,    0,  595,
  595,    0,    0,    0,    0,    0,  595,    0,  595,  595,
  595,    0,    0,    0,    0,    0,    0,  595,  595,  385,
    0,  385,  385,    0,    0,    0,  595,  595,  385,  385,
  385,    0,  385,  595,  385,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  385,  385,    0,    0,  385,    0,
    0,  385,    0,  385,  385,  385,  385,  385,    0,  385,
  385,  385,  385,    0,  385,  385,    0,  385,  385,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  385,  385,  385,  385,    0,    0,  385,    0,
    0,    0,    0,    0,    0,  385,  385,    0,    0,  385,
    0,  385,    0,    0,    0,    0,    0,    0,    0,    0,
  385,    0,  385,  385,  385,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  385,    0,    0,  385,    0,
  385,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   73,    0,   74,   75,    0,    0,    0,    0,    0,    0,
    0,  187,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  148,  385,    0,    0,    0,
    0,    0,    0,   73,    0,   74,   75,    0,    0,    0,
    0,    0,    0,    0,  301,    0,    0,    0,    0,    0,
    0,    0,    0,  385,  292,  293,    0,    0,  148,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  385,  385,  385,  385,  385,  385,  385,  385,    0,  385,
    0,  385,  385,  385,  385,    0,    0,    0,    0,  302,
    0,    0,    0,    0,    0,  303,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  385,
  385,    0,    0,    0,  385,    0,  385,  385,    0,  311,
    0,  385,    0,  312,  313,  314,  315,  316,  317,    0,
  318,  319,  320,  321,  322,  323,  324,  149,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  276,    0,  276,  276,    0,    0,  385,    0,
    0,    0,  385,  385,  150,    0,    0,    0,    0,    0,
  149,    0,    0,    0,    0,    0,    0,  276,    0,    0,
    0,  151,  152,  153,  154,  155,  156,  157,  158,    0,
  159,    0,  160,  161,  162,  163,    0,  150,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  151,  152,  153,  154,  155,  156,
  157,  158,    0,  159,    0,  160,  161,  162,  163,    0,
  188,  165,    0,    0,    0,  166,    0,  167,  168,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  188,  165,  294,  295,  296,  166,    0,
  167,  168,    0,    0,    0,    0,    0,    0,    0,  170,
    0,  276,    0,  171,  172,  276,  276,  276,  276,  276,
  276,    0,  276,  276,  276,  276,  276,  276,  276,  276,
    0,    0,   73,    0,   74,   75,    0,    0,    0,    0,
    0,    0,  170,  301,    0,    0,  171,  172,    0,    0,
    0,    0,    0,  596,  293,    0,  276,  148,    0,    0,
   73,    0,   74,   75,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  276,  276,  276,  276,  276,  276,  276,
  276,  339,  276,    0,  276,  276,  276,  276,  302,    0,
    0,    0,    0,  133,  303,  133,  133,    0,    0,    0,
    0,    0,    0,    0,  133,    0,    0,    0,   73,    0,
   74,   75,    0,    0,  133,  133,    0,    0,  133,  147,
    0,    0,  276,  276,    0,    0,    0,  276,  340,  276,
  276,    0,    0,  148,    7,    8,    0,    0,  341,    0,
    0,    9,    0,    0,    0,    0,    0,   11,   12,  133,
    0,    0,    0,    0,   13,  133,    0,    0,    0,    0,
    0,    0,  342,  343,  344,   17,   18,    0,  345,    0,
    0,  276,    0,  346,    0,  276,  276,    0,    0,  149,
    0,  127,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  128,  129,    0,    0,  130,    0,
  131,    0,    0,    0,    0,    0,  150,  132,    0,  133,
  134,  135,  136,  137,  138,  139,  140,    0,  141,  142,
  143,    0,    0,  151,  152,  153,  154,  155,  156,  157,
  158,    0,  159,    0,  160,  161,  162,  163,    0,    0,
  133,   73,    0,   74,   75,    0,  690,    0,  691,    0,
    0,    0,  187,    0,    0,  149,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  148,  133,    0,    0,
    0,    0,  188,  165,  294,  295,  296,  166,    0,  167,
  168,    0,  150,    0,  133,  133,  133,  133,  133,  133,
  133,  133,    0,  133,    0,  133,  133,  133,  133,  151,
  152,  153,  154,  155,  156,  157,  158,    0,  159,    0,
  160,  161,  162,  163,    0,    0,    0,   73,    0,   74,
   75,  170,    0,    0,    0,  171,  172,    0,  147,    0,
    0,    0,    0,  133,  133,  133,  133,  133,  133,    0,
  133,  133,  148,    0,    0,  150,  144,    0,  188,  165,
    0,    0,  145,  166,    0,  167,  168,    0,    0,    0,
    0,  697,  151,  152,  153,  154,  155,  156,  157,  158,
    0,  159,    0,  160,  161,  162,  163,    0,    0,  698,
    0,  699,  133,    0,    0,    0,  133,  133,  149,   73,
    0,   74,   75,    0,    0,    0,    0,  170,    0,    0,
  147,  171,  172,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  148,  150,  166,    0,  167,  168,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  151,  152,  153,  154,  155,  156,  157,  158,
    0,  159,    0,  160,  161,  162,  163,    0,   73,    0,
   74,   75,    0,    0,    0,    0,    0,    0,    0,  488,
    0,    0,    0, 1046,  149,  172,    0,    0,    0,    0,
    0,    0,    0,  148,    0,    0,    0,    0,    0,    0,
    0,  188,  165,    0,    0,    0,  166,    0,  167,  168,
    0,  150,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  151,  152,
  153,  154,  155,  156,  157,  158,    0,  159,    0,  160,
  161,  162,  163,    0,    0,    0,   73,    0,   74,   75,
  170,    0,    0,    0,  171,  172,  149,  147,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  148,    0,    0,    0,    0,    0,  164,  165,    0,
    0,    0,  166,  150,  167,  168,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  169,    0,
  151,  152,  153,  154,  155,  156,  157,  158,    0,  159,
    0,  160,  161,  162,  163,  149,   73,    0,   74,   75,
    0,    0,    0,    0,    0,    0,  170,  727,    0,    0,
  171,  172,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  148,  150,    0,    0,    0,    0,    0,    0,  188,
  165,    0,    0,    0,  166,    0,  167,  168,    0,  151,
  152,  153,  154,  155,  156,  157,  158,    0,  159,    0,
  160,  161,  162,  163,    0,   73,    0,   74,   75,    0,
    0,    0,    0,    0,    0,    0, 1151,    0,    0,    0,
    0,    0,    0,  149,    0,    0,    0,    0,  170,    0,
  148,    0,  171,  172,    0,    0,    0,    0,  188,  165,
    0,    0,    0,  166,    0,  167,  168,    0,    0,    0,
  150,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  151,  152,  153,
  154,  155,  156,  157,  158,    0,  159,    0,  160,  161,
  162,  163,    0,  172,    0,  172,  172,  170,    0,    0,
    0,  171,  172,  149,  172,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  172,    0,
    0,    0,    0,    0,    0,    0,  548,  165,    0,    0,
  150,  166,    0,  167,  168,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  151,  152,  153,
  154,  155,  156,  157,  158,    0,  159,    0,  160,  161,
  162,  163,  149,  386,    0,  386,  386,    0,    0,    0,
    0,    0,    0,    0,  386,  170,    0,    0,    0,  171,
  172,    0,    0,    0,    0,    0,    0,    0,  386,  150,
    0,    0,    0,    0,    0,    0,  188,  165,    0,    0,
    0,  166,    0,  167,  168,    0,  151,  152,  153,  154,
  155,  156,  157,  158,    0,  159,    0,  160,  161,  162,
  163,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  172,   43,    0,   43,   43,  170,    0,    0,    0,  171,
  172,    0,    0,    0,    0,  188,  165,    0,    0,    0,
  166,    0,  167,  168,    0,    0,    0,  172,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  172,  172,  172,  172,  172,  172,
  172,  172,    0,  172,    0,  172,  172,  172,  172,    0,
    0,    0,    0,    0,  170,    0,    0,    0,  171,  172,
  386,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  172,  172,    0,    0,  386,  172,    0,
  172,  172,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  386,  386,  386,  386,  386,  386,
  386,  386,   43,  386,    0,  386,  386,  386,  386,    0,
    0,    0,    0,    0,    0,   43,   43,    0,    0,   43,
    0,   43,  172,    0,    0,    0,  172,  172,   43,    0,
   43,   43,   43,   43,   43,   43,   43,   43,    0,   43,
   43,   43,    0,  386,  386,  507,    0,  507,  386,    0,
  386,  386,    0,  675,  507,  507,  675,    0,    0,  507,
    0,    0,  675,    0,    0,    0,  675,  675,    0,  675,
    0,    0,    0,    0,    0,    0,    0,  507,    0,  507,
  507,  507,  507,  507,    0,  507,  507,  507,  507,    0,
  507,  507,  386,  507,  507,    0,  386,  386,    0,    0,
    0,    0,    0,    0,    0,  801,    0,    0,  507,  507,
  507,  507,    0,    0,  507,    0,    0,    0,    0,    0,
    0,  507,  507,    0,    0,  507,    0,  507,    0,    0,
    0,    0,    0,    0,    0,    0,  507,    0,  507,  507,
  507,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  507,    0,  488,  507,  488,  507,    0,    0,    0,
    0,  675,  488,  488,  675,    0,    0,   43,    0,    0,
  675,    0,    0,   43,  675,  675,    0,  675,    0,    0,
    0,    0,    0,    0,    0,  488,    0,  488,  488,  488,
  488,  488,    0,  488,  488,  488,  488,    0,  488,  488,
    0,  488,  488,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  488,  488,  488,  488,
    0,    0,  488,    0,    0,    0,    0,    0,    0,  488,
  488,    0,    0,  488,    0,  488,    0,    0,    0,    0,
    0,    0,    0,    0,  488,    0,  488,  488,  488,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  488,
    0,    0,  488,  615,  488,    0,    0,    0,    0,    0,
    0,    0,  802,  803,    0,  804,  805,  806,  807,  808,
  809,  810,  811,  812,  813,    0,    0,  675,  675,  675,
  814,  815,  816,  817,  818,    0,    0,  819,  820,  616,
  821,  822,    0,    0,  617,    0,    0,    0,  823,    0,
  618,  824,  825,  826,  827,  828,  829,  830,  831,  832,
  833,  834,  835,  836,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  619,  620,    0,    0,  621,  622,
  623,  624,  625,  626,  627,  628,  629,  630,  631,  632,
  633,    0,  634,  635,  636,  637,  638,  639,  640,  641,
  642,  643,  644,  645,  646,  647,  648,  649,  650,  651,
   73,    0,   74,  652,    0,    0,    0,    0,    0,  377,
  378,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  653,    0,    0,    0,    0,  675,  675,  675,    0,    0,
    0,    0,  379,    0,  380,  381,  382,  383,  384,    0,
  385,  386,  387,  388,    0,  389,  390,    0,  391,  392,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    7,    8,  393,  394,    0,    0,  395,
    0,    0,    0,    0,    0,    0,   11,   12,    0,    0,
  396,    0,  397,    0,    0,    0,    0,   73,    0,   74,
    0,  398,    0,  399,   17,   18,  377,  740,    0,    0,
    0,    0,    0,    0,    0,    0,  400,    0,    0,  401,
    0,  402,    0,    0,    0,    0,    0,    0,    0,  379,
    0,  380,  381,  382,  383,  384,    0,  385,  386,  387,
  388,    0,  389,  390,    0,  391,  392,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    7,    8,  393,  394,    0,    0,  395,    0,    0,    0,
    0,    0,    0,   11,   12,    0,    0,  396,    0,  397,
    0,    0,    0,    0,  490,    0,  490,    0,  398,    0,
  399,   17,   18,  490,  490,    0,    0,    0,    0,    0,
    0,    0,    0,  400,    0,    0,  401,    0,  402,    0,
    0,    0,    0,    0,    0,    0,  490,    0,  490,  490,
  490,  490,  490,    0,  490,  490,  490,  490,    0,  490,
  490,    0,  490,  490,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  490,  490,  490,
  490,    0,    0,  490,    0,    0,    0,    0,    0,    0,
  490,  490,    0,    0,  490,    0,  490,    0,    0,    0,
    0,  457,    0,  457,    0,  490,    0,  490,  490,  490,
  457,  457,    0,    0,    0,    0,    0,    0,    0,    0,
  490,    0,    0,  490,    0,  490,    0,    0,    0,    0,
    0,    0,    0,  457,    0,  457,  457,  457,  457,  457,
    0,  457,  457,  457,  457,    0,  457,  457,    0,  457,
  457,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  457,  457,  457,  457,    0,    0,
  457,    0,    0,    0,    0,    0,    0,  457,  457,    0,
    0,  457,    0,  457,    0,    0,    0,    0,  427,    0,
  427,    0,  457,    0,  457,  457,  457,  427,  427,    0,
    0,    0,    0,    0,    0,    0,    0,  457,    0,    0,
  457,    0,  457,    0,    0,    0,    0,    0,    0,    0,
  427,    0,  427,  427,  427,  427,  427,    0,  427,  427,
  427,  427,    0,  427,  427,    0,  427,  427,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  427,  427,  427,  427,    0,    0,  427,    0,    0,
    0,    0,    0,    0,  427,  427,    0,    0,  427,    0,
  427,    0,    0,    0,    0,  295,    0,  295,    0,  427,
    0,  427,  427,  427,  295,  295,    0,    0,    0,    0,
    0,    0,    0,    0,  427,    0,    0,  427,    0,  427,
    0,    0,    0,    0,    0,    0,    0,  295,    0,  295,
  295,  295,  295,  295,    0,  295,  295,  295,  295,    0,
  295,  295,    0,  295,  295,    0,    0,    0,    0,  338,
    0,    0,    0,    0,    0,    0,    0,    0,  295,  295,
  295,  295,    0,    0,  295,    0,    0,    0,    0,    0,
    0,  295,  295,    0,    0,  295,    0,  295,    0,    0,
    0,    0,    0,    0,    0,    0,  295,    0,  295,  295,
  295,    3,    0,    0,    0,    0,    4,    0,    0,    5,
    6,  295,    7,    8,  295,    0,  295,    0,    0,    9,
   10,    0,    0,    0,    0,   11,   12,    0,    0,    0,
    0,    0,   13,    0,   14,   15,   16,    3,    0,    0,
    0,    0,    4,   17,   18,    5,    6,    0,    7,    8,
    0,    0,   19,   20,    0,    9,   10,    0,    0,   21,
    0,   11,   12,    0,    0,    0,    0,    0,   13,    0,
   14,   15,   16,    0,    0,    0,    0,    0,    0,   17,
   18,    0,    0,    0,    0,    0,    0,    0,   19,   20,
    0,    0,    0,    0,    0,   21,
  };
  protected static readonly short [] yyCheck = {             6,
   14,    7,   16,   61,  165,   12,   48,   96,  246,  486,
  232,  232,  220,   20,  852,  287,  384,  189,  290,  232,
  253,  453,  259,  258,  785,  260,  261,  103,  267,   97,
  271,  253,  253,  254,   65,   19,   50,  268,  282,  232,
  253,  254,  255,  256,  257,  269,  232,  267,  190,  267,
   56,   57,  972,  281,  258,   69,  260,  261,  264,   66,
  253,   68,   76,  266,  904,   79,   72,  253,  266,  258,
  259,  260,  261,  258,  263,  260,  261,  370,  268,  121,
  269,  273,  258,  273,  260,  261,  228,  105,  106,  107,
  108,  109,  110,  100,  560,  259,  288,  563,  263,  258,
  189,  260,  261,  271,  258,  147,  260,  261,  263,  251,
  269,  169,  126,  281,  278,  269,  277,  148,  286,  284,
  259,  324,  190,  272,  283,  279,  280,  371,  170,  269,
  267,  892,  272,  273,  271,  193,  263,  264,  269,  278,
  280,  148,  273,  371,  382,  187,  259,  273,  266,  280,
  260,  583,  320,  451,  452,  453,  454,  263,  164,  591,
  453,  454,  288,  924,  274,  278,  263,  400,  258,  287,
  260,  261,  370,  334,  272,  189,  258,  475,  260,  261,
  548,  279,  188,  267,  258,  269,  260,  261,  518,  458,
  459,  258,  199,  260,  261,  279,  280, 1117,  263,  288,
  420,  421,  377,  421,  235,  270,  475,  258,  222,  260,
  263,  264,  263,  270,  507,  508,  281,  258,  271,  260,
  261,  982,  253,  269,  281,  266,  267,  285,  269,  236,
  272,  238,  283,  240,  383,  384,  385,  386,  279,  280,
  382,  282,  300,  274,  271,  259, 1166,  289,  271,  455,
  456,  278,  272,  342, 1094,  278,  298,  288,  242,  301,
 1098,  281,  330,  412,  413,  414,  415,  274,  273,  343,
  511,  270,  509,  331,  273,  741,  343,  377,  517,  428,
  504,  430,  431,  288,  259,  273,  362,  377,  519,  520,
  258,  297,  260,  261,  325,  263,  271,  386,    0, 1060,
  288,  263,  537,  272,  273,  373,  374,  479,  270,  398,
  392,  393,  473,  474,  388,  389,  390,  489,  270,  387,
  270,  278,  284,  556,  281,  558,  598,  385,  455,  456,
  282,  499,  282,  537,  556,  556,  343,  273,  770,  346,
  499,  271,  260,  556,  484,  281,  500,  501,  502,  380,
  418,  545,  383,  484,  451,  452,  453,  542,  426,  270,
  391,  392,  270,  556,  553,  554,  542,  269,  845,  400,
  556,  282,  461,  380,  282,  343,  383,  270,  258,  547,
  260,  261,  388,  282,  391,  392,  393,  272,  287,  282,
  397,  272,  534,  400,  401, 1156,  281,  381,  382,  267,
  281,  443,  455,  456,  462,  269, 1167,  268,  272,  498,
  559,  267,  273,  420,  456,  279,  500,  501,  502,  266,
  558,  435,  429,  565,  271,  567,  440,  569,  570,  269,
  572,  573,  272,  517,  258,  477,  260,  267,  258,  279,
  260,  261,  267,  532,  502,  269,  488,  461,  378,  379,
  380,  381,  382,  267,  269,  279,  280,  272,  267,  500,
  501,  502,  267,  343,  279,  479,  608,  498,  262,  263,
  501,  258,  530,  260,  261,  489,    0,  268,  536,  701,
  702,  262,  263,  258,  916,  260,  261,  545,  701,  702,
  508,  509,  510,  511,  272,  273,  514,  515,  258,  557,
  260,  532,  870,  871,  872,  873,  778,  538,  701,  702,
  278,  879,  880,  881,  258,  657,  260,  261,  679,  310,
  273,  268,  694,  684,  315,  556,  269,  558,  271,  270,
  321,  538,  273,  258,  266,  260,  578,  544,  329,  314,
  608,  271,  548,  550,  269,  320,  279,  451,  452,  453,
  454,  558,  594,  279,  279,  280,  614,  564,  589,  282,
  602,  575,  576,  735,  772,  270,  268,  609,  273,  272,
  601,  475,  392,  393,  321,  258,  278,  260,  720,  787,
  269,  258,  271,  260,  267,  332,  383,  384,  385,  386,
  337,  659,  269,  269,  267,  271,  603,  280,  418,  270,
    0,  773,  273,  499,  268,  392,  393,  280,  310,  356,
  357,  679,  269,  315,  270,  272,  318,  319,  365,  321,
  322,  260,  279,  325,  655,  278,  328,  329,  717,  272,
  273,  418,  334,  335,  258,  271,  260,  261,  269,  341,
  271,  343,  344,  345,  269,  269,  271,  349,  350,  351,
  352,  353,  271,  355,  685,  279,  280,  321,  360,  361,
  362,  269,  387,  271,  281,  268,  368,  331,  270,  267,
  334,  335,  679,  731,  732,  733,  500,  501,  502,  693,
  694,  269,  280,  271,  348,  727,  700,  275,  695,  271,
  314,  279,  280,  268,  282,  359,  320,  728,  269,  287,
  269,  272,  271,  717,  444,  445,  269,  738,  279,  272,
  279,  280,  271,  258,  745,  260,  279,  775,  321,  271,
  727,  735,  272,  273,  792,  258,  420,  260,  261,  332,
  737,  287,  739,  475,  337,  310,  269,  271,  745,  271,
  315,  271,    0,  258,  268,  260,  321,  754,  263,  352,
  353,  271,  759,  356,  329,  269,  763,  788,  275,  773,
  272,  273,  365,  387,  972,  279,  280,  332,  451,  452,
  453,  454,  455,  456,  842,  500,  501,  502,  451,  452,
  453,  454,  455,  456,  556,  557,  310,  556,  557, 1027,
 1028,  315,  519,  520,  318,  319,  269,  321,  322,  553,
  554, 1009,  272,  273,  328,  329,  279,  280,  332,  273,
  334,  335,  260,  902,  271,  272,  273,  341,  960,  343,
  344,  345,  505,  506,  259,  258,  509,  260,  352,  353,
  263,  838,  505,  506,  267,  504,  509,  361,  362,  382,
  258,  899,  260,  261,  368,  272,  273,    0,  272,  273,
  259,  269,  267,  451,  452,  453,  454,  455,  456,  890,
  272,  273,  920,  894,  267, 1037,  272,  273,  268,  900,
  272,  929,  271,  941,  272,  273,  500,  501,  502,  272,
 1052,  272,  273,  914,  272, 1046,  272,  894,  902,  432,
  433,  434,  435,  436,  437,  438,  439,  440,  441,  378,
  379,  380,  381,  382,  265,  272,  273,  505,  506, 1117,
  310,  509,  500,  501,  502,  315,  272,  273,  318,  319,
  268,  321,  322,  272,  273,  325,  933,  934,  328,  329,
  268,  500,  501,  502,  334,  335,  272,  273,  945,  272,
  268,  341,  271,  343,  344,  345, 1014,  272,  273,  349,
  350,  351,  352,  353,  272,  355,  272,  273, 1166,  272,
  360,  361,  362,  994,  272,  273,  973,  974,  368,  272,
  273, 1002,  310,  321,  980,  271,  269, 1008, 1150,  274,
  308,  538, 1013,  321,  332,  377,  500,  501,  502,  337,
 1048,  329,  278,  321, 1001,  278, 1003, 1004,  278,  278,
  543,  377,  330,  377, 1093, 1147,  334,  335,  356,  357,
  268,  421,  268,  268, 1021,  275,  284,  365,  284,  278,
  348,  278,  260, 1037,  392,  393,  394,  500,  501,  502,
  358,  399,  400,  401,  259,  370,  278,  272, 1052, 1023,
 1024, 1025, 1026, 1027, 1028, 1029, 1030, 1031, 1032, 1033,
 1034,  275,  310,  271,  310,  310,  275,  315,  512,  315,
  318,  319,  512,  321,  322,  321,  321,  274, 1126,  274,
  328,  329,  274,  329,  329,  273,  334,  335,  271, 1086,
 1087, 1088,  272,  341,  275,  343,  344,  345,  272, 1096,
 1097,  271,  479,  466,  352,  353,  473,  271,  450,  280,
  278,  274,  274,  361,  362,  258,  270,  260,  261, 1151,
  368,  270,  270,    0,  267,  268,  269,  270,  271,  272,
  273, 1152,  275,  271,  287,  278,  279,  280,  267,  282,
  283,  284,  272,  275,  287,  288, 1150,  290,  272,  292,
  293,  294,  295,  296, 1151,  298,  299,  300,  301,  272,
  303,  304,  272,  306,  307,  270,  270,  310,  284,  271,
  284,  314,  315,  269,  269,  318,  319,  320,  321,  322,
  323,  324,  325,  324,  327,  328,  329,  271,  271,  332,
  271,  334,  335,  510,  337,  338,  270,  340,  341,  371,
  343,  344,  345,  270,  274,  512,  349,  350,  351,  352,
  353,  512,  355,  356,  274,  272,  270,  360,  361,  362,
  273,  364,  365,  271,  367,  368,  369,  370,  371,  451,
  452,  453,  454,  271,  271,  450,  451,  452,  453,  454,
  455,  456,  271,  271,  387,  460,  461,  462,  463,  271,
  271,  271,  268,  513,  514,  515,  516,  271,  271,  271,
  271,  258,  405,  260,  261,  271,  263,  392,  393,  394,
  275,  271,  270,  270,  399,  400,  401,  420,  421,  270,
  451,  452,  453,  454,  455,  456,  270,  284,  274,  432,
  505,  506,  272,  272,  509,  260,  274,  274,  272,  274,
  271,  274,  270,  272,  271,  321,  449,  450,  451,  452,
  453,  454,  455,  456,  272,  458,  332,  460,  461,  462,
  463,  337,  258,  272,  260,  261,  268,  272,  272,  272,
  278,  546,  272,  269,  505,  506,  352,  353,  509,  272,
  356,  272,  272,  279,  280,  272,  343,  283,  272,  365,
  272,  272,  272,  287,  272,  498,  499,  500,  501,  502,
  503,  272,  505,  506,  272,  275,  272,  510,  310,  271,
  260,  270,  272,  315,  517,  273,  288,  271,  271,  321,
  272,  258,    0,  260,  261,  267,  267,  329,  267,    0,
  267,  268,  269,  270,  271,  272,  273,  267,  275,  267,
  273,  278,  279,  280,  547,  282,  283,  272,  551,  552,
  287,  288,  267,  290,  272,  292,  293,  294,  295,  296,
  267,  298,  299,  300,  301,  267,  303,  304,  271,  306,
  307,  271,  267,  310,  272,  267,  267,  314,  315,  267,
  101,  318,  319,  320,  321,  322,  323,  324,  325,  271,
  327,  328,  329,  232,  232,  332,  232,  334,  335,  678,
  337,  338,  764,  340,  341,  914,  343,  344,  345,  405,
  581,  232,  349,  350,  351,  352,  353,  605,  355,  356,
  979, 1123,  414,  360,  361,  362, 1022,  364,  365, 1153,
  367,  368,  369, 1009,  371, 1013,  432,  732,  899,  561,
  389,  297,   18,  977, 1090,  684,  681,  844,   47,   -1,
  387,  521,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,   -1,  458,   -1,  460,  461,  462,  463,  405,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  450,  451,  452,
  453,  454,  455,  456,  421,   -1,   -1,  460,  461,  462,
  463,   -1,   -1,   -1,   -1,  432,   -1,   -1,   -1,   -1,
   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,  505,
  506,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,  258,   -1,
  260,  261,  505,  506,   -1,   -1,  509,   -1,   -1,  269,
   -1,   -1,  272,  273,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  547,   -1,  283,   -1,  551,  552,   -1,   -1,   -1,
   -1,  498,  499,  500,  501,  502,  503,   -1,  505,  506,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  552,
  517,   -1,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,    0,  267,  268,  269,  270,
  271,  272,  273,   -1,  275,   -1,   -1,  278,  279,  280,
  547,  282,  283,   -1,  551,  552,  287,  288,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,
   -1,   -1,   -1,  314,  315,   -1,   -1,  318,  319,  320,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,   -1,
   -1,  332,   -1,  334,  335,   -1,  337,  338,   -1,  340,
  341,   -1,  343,  344,  345,  405,   -1,   -1,  349,  350,
  351,  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,
  361,  362,   -1,  364,  365,   -1,  367,  368,  369,   -1,
  371,   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  387,   -1,   -1,  449,
  450,  451,  452,  453,  454,  455,  456,   -1,  458,   -1,
  460,  461,  462,  463,  405,  521,  522,  523,  524,  525,
  526,  527,  528,  529,  530,  531,  532,  533,  534,  535,
  421,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,
   -1,   -1,   -1,  503,   -1,  505,  506,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,  258,   -1,  260,  261,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  269,   -1,  271,   -1,  273,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  547,   -1,  283,
   -1,  551,  552,   -1,   -1,   -1,   -1,  498,  499,  500,
  501,  502,  503,   -1,  505,  506,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  517,   -1,   -1,   -1,
   -1,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,
   -1,    0,  267,  268,  269,  272,  271,  272,  273,   -1,
  275,   -1,   -1,  278,  279,  280,  547,   -1,  283,   -1,
  551,  552,  287,  288,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,  314,
  315,   -1,   -1,  318,  319,  320,  321,  322,  323,  324,
  325,   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,
  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,
  345,  405,   -1,   -1,  349,  350,  351,  352,  353,   -1,
  355,  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,
  365,   -1,  367,  368,  369,   -1,   -1,   -1,  432,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  383,  384,  385,  386,
   -1,   -1,  387,   -1,   -1,  449,  450,  451,  452,  453,
  454,  455,  456,   -1,  458,   -1,  460,  461,  462,  463,
  405,   -1,   -1,   -1,   -1,  412,  413,  414,  415,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  421,   -1,   -1,   -1,
   -1,  428,   -1,  430,  431,   -1,   -1,  432,   -1,   -1,
   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,  503,
   -1,  505,  506,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  269,   -1,   -1,  272,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  547,   -1,  283,   -1,  551,  552,   -1,
   -1,   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,
  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  517,   -1,   -1,   -1,   -1,   -1,   -1,  258,
   -1,  260,  261,   -1,   -1,   -1,   -1,    0,  267,  268,
  269,  272,  271,  272,  273,   -1,  275,   -1,   -1,  278,
  279,  280,  547,   -1,  283,   -1,  551,  552,  287,  288,
   -1,  290,  559,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,  310,   -1,   -1,   -1,  314,  315,   -1,   -1,  318,
  319,  320,  321,  322,  323,  324,  325,   -1,  327,  328,
  329,   -1,   -1,  332,   -1,  334,  335,   -1,  337,  338,
   -1,  340,  341,   -1,  343,  344,  345,  405,   -1,   -1,
  349,  350,  351,  352,  353,   -1,  355,  356,   -1,   -1,
   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,  368,
  369,   -1,   -1,   -1,  432,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  383,  384,  385,  386,   -1,   -1,  387,   -1,
   -1,  449,  450,  451,  452,  453,  454,  455,  456,   -1,
  458,   -1,  460,  461,  462,  463,  405,   -1,   -1,   -1,
   -1,  412,  413,  414,  415,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  421,   -1,   -1,   -1,   -1,  428,   -1,  430,
  431,   -1,   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,
  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,
  449,  450,  451,  452,  453,  454,  455,  456,   -1,  458,
   -1,  460,  461,  462,  463,   -1,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  547,
   -1,  283,   -1,  551,  552,   -1,   -1,   -1,   -1,  498,
  499,  500,  501,  502,  503,   -1,  505,  506,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  517,   -1,
   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,
   -1,   -1,    0,   -1,  267,  268,  269,   -1,  271,  272,
  273,   -1,  275,   -1,   -1,  278,  279,  280,  547,   -1,
  283,   -1,  551,  552,  287,  288,   -1,  290,  559,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,
   -1,  314,  315,   -1,   -1,  318,  319,  320,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,  332,
   -1,  334,  335,   -1,  337,  338,    0,  340,  341,   -1,
  343,  344,  345,  405,   -1,   -1,  349,  350,  351,  352,
  353,  272,  355,  356,   -1,   -1,   -1,  360,  361,  362,
   -1,  364,  365,   -1,  367,  368,  369,   -1,   -1,   -1,
  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  387,   -1,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,   -1,  458,   -1,  460,  461,
  462,  463,  405,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  421,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  432,
   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,
   -1,  503,   -1,  505,  506,   -1,  449,  450,  451,  452,
  453,  454,  455,  456,   -1,  458,   -1,  460,  461,  462,
  463,   -1,  383,  384,  385,  386,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  547,   -1,   -1,   -1,  551,
  552,  412,  413,  414,  415,  498,  499,  500,  501,  502,
  503,   -1,  505,  506,   -1,   -1,   -1,  428,   -1,  430,
  431,   -1,   -1,   -1,  517,   -1,   -1,   -1,   -1,   -1,
  258,   -1,  260,  261,   -1,   -1,   -1,    0,   -1,  267,
  268,  269,   -1,  271,  272,  273,   -1,  275,   -1,   -1,
   -1,  279,  280,   -1,  547,  283,   -1,   -1,  551,  552,
  288,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,  310,   -1,   -1,   -1,  314,  315,   -1,   -1,
  318,  319,  320,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,   -1,   -1,  332,   -1,  334,  335,   -1,  337,
  338,    0,  340,  341,  268,  343,  344,  345,   -1,   -1,
   -1,  349,  350,  351,  352,  353,   -1,  355,  356,   -1,
   -1,   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,
  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,  559,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  310,   -1,   -1,  387,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,   -1,
   -1,   -1,   -1,   -1,  328,  329,   -1,  405,   -1,   -1,
  334,  335,   -1,   -1,   -1,   -1,   -1,  341,   -1,  343,
  344,  345,   -1,  421,   -1,   -1,   -1,   -1,  352,  353,
   -1,   -1,   -1,   -1,  432,   -1,   -1,  361,  362,   -1,
   -1,   -1,   -1,   -1,  368,   -1,   -1,   -1,   -1,   -1,
   -1,  449,  450,  451,  452,  453,  454,  455,  456,   -1,
  458,   -1,  460,  461,  462,  463,   -1,  378,  379,  380,
  381,  382,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  391,  392,  393,  394,   -1,  396,  397,  398,  399,  400,
  401,  402,   -1,   -1,   -1,   -1,   -1,   -1,  409,   -1,
  498,  499,  500,  501,  502,  503,   -1,  505,  506,   -1,
   -1,  422,  423,  424,  425,  426,  427,   -1,   -1,  517,
   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,
    0,   -1,   -1,   -1,  267,  268,  269,   -1,  271,  272,
  273,   -1,  275,   -1,   -1,   -1,  279,  280,   -1,  547,
  283,   -1,   -1,  551,  552,  288,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,
   -1,  314,  315,   -1,   -1,  318,  319,  320,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,  332,
   -1,  334,  335,   -1,  337,  338,   -1,  340,  341,  268,
  343,  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,
  353,   -1,  355,  356,   -1,   -1,   -1,  360,  361,  362,
   -1,  364,  365,   -1,  367,  368,  369,  548,  549,   -1,
   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  310,   -1,   -1,  387,   -1,  315,   -1,   -1,  318,
  319,   -1,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,
  329,   -1,  405,   -1,   -1,  334,  335,   -1,   -1,   -1,
   -1,   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
   -1,   -1,   -1,  352,  353,   -1,   -1,   -1,   -1,  432,
   -1,   -1,  361,  362,   -1,   -1,   -1,   -1,   -1,  368,
   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,  451,  452,
  453,  454,  455,  456,   -1,  458,   -1,  460,  461,  462,
  463,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  498,  499,  500,  501,  502,
  503,   -1,  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  517,   -1,   -1,   -1,  258,   -1,
  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  547,   -1,   -1,   -1,  551,  552,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,  308,   -1,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
   -1,  321,  322,  323,  324,  325,   -1,  327,  328,  329,
  330,  331,  332,   -1,  334,  335,    0,  337,  338,   -1,
  340,  341,   -1,  343,  344,  345,   -1,   -1,  348,  349,
  350,  351,  352,  353,   -1,  355,  356,  357,  358,  359,
  360,  361,  362,   -1,  364,  365,   -1,  367,  368,  369,
  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,
  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
  308,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,
  318,  319,   -1,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,  330,  331,  332,   -1,  334,  335,    0,  337,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,
  348,  349,  350,  351,  352,  353,   -1,  355,  356,  357,
  358,  359,  360,  361,  362,   -1,  364,  365,   -1,  367,
  368,  369,  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,
   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,  308,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,  330,  331,  332,   -1,  334,  335,
    0,  337,  338,   -1,  340,  341,   -1,  343,  344,  345,
   -1,   -1,  348,  349,  350,  351,  352,  353,   -1,  355,
  356,  357,  358,  359,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,  258,   -1,  260,   -1,   -1,   -1,
   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,  308,   -1,  310,   -1,   -1,   -1,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,   -1,  327,  328,  329,  330,  331,  332,   -1,
  334,  335,    0,  337,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,  348,  349,  350,  351,  352,  353,
   -1,  355,  356,  357,  358,  359,  360,  361,  362,   -1,
  364,  365,   -1,  367,  368,  369,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,  271,
  272,   -1,   -1,  275,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,
   -1,    0,   -1,  315,   -1,   -1,  318,  319,   -1,  321,
  322,  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,
  332,   -1,  334,  335,   -1,  337,  338,   -1,  340,  341,
   -1,  343,  344,  345,   -1,   -1,   -1,  349,  350,  351,
  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,  361,
  362,   -1,  364,  365,   -1,  367,  368,  369,  258,   -1,
  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,  308,    0,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
   -1,  321,  322,  323,  324,  325,   -1,  327,  328,  329,
  330,  331,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,
  340,  341,   -1,  343,  344,  345,   -1,   -1,  348,  349,
  350,  351,  352,  353,   -1,  355,   -1,   -1,  358,  359,
  360,  361,  362,   -1,  364,   -1,   -1,  367,  368,  369,
  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,
  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,    0,  303,  304,   -1,  306,  307,
  308,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,
  318,  319,   -1,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,  330,  331,   -1,   -1,  334,  335,   -1,   -1,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,  258,
  348,  349,  350,  351,  352,  353,   -1,  355,  267,  268,
  358,  359,  360,  361,  362,   -1,  364,   -1,   -1,  367,
  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,  308,
   -1,  310,   -1,   -1,   -1,   -1,  315,    0,   -1,  318,
  319,   -1,  321,  322,  323,  324,  325,   -1,  327,  328,
  329,  330,  331,   -1,   -1,  334,  335,   -1,   -1,  338,
   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,  348,
  349,  350,  351,  352,  353,   -1,  355,   -1,   -1,  358,
  359,  360,  361,  362,   -1,  364,   -1,  258,  367,  368,
  369,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,  308,   -1,  310,
   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,    0,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,  330,
  331,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,
  341,   -1,  343,  344,  345,   -1,   -1,  348,  349,  350,
  351,  352,  353,   -1,  355,   -1,   -1,  358,  359,  360,
  361,  362,   -1,  364,   -1,   -1,  367,  368,  369,   -1,
   -1,  267,  268,   -1,   -1,  271,  272,   -1,   -1,  275,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,    0,   -1,  332,   -1,  334,  335,
   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,  345,
   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,  267,  268,   -1,   -1,  271,  272,
   -1,   -1,  275,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,
   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,  332,
   -1,  334,  335,   -1,  337,  338,    0,  340,  341,   -1,
  343,  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,
  353,   -1,  355,  356,   -1,   -1,   -1,  360,  361,  362,
   -1,  364,  365,   -1,  367,  368,  369,  258,   -1,  260,
   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,
   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,   -1,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,   -1,
   -1,  332,   -1,  334,  335,   -1,  337,  338,    0,  340,
  341,   -1,  343,  344,  345,   -1,   -1,   -1,  349,  350,
  351,  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,
  361,  362,   -1,  364,  365,   -1,  367,  368,  369,   -1,
   -1,   -1,  258,   -1,  260,   -1,   -1,   -1,   -1,   -1,
   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
    0,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,    0,  332,   -1,  334,  335,
   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,  345,
   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,  258,   -1,  260,   -1,   -1,   -1,
   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,  273,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,   -1,  327,  328,  329,   -1,   -1,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,
   -1,  355,   -1,   -1,   -1,   -1,  360,  361,  362,   -1,
  364,   -1,   -1,  367,  368,  369,  258,   -1,  260,   -1,
   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,
   -1,   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,
  322,  323,  324,  325,   -1,  327,  328,  329,  268,   -1,
   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,  341,
   -1,  343,  344,  345,   -1,   -1,   -1,  349,  350,  351,
  352,  353,   -1,  355,   -1,   -1,   -1,   -1,  360,  361,
  362,   -1,  364,   -1,   -1,  367,  368,  369,   -1,   -1,
  310,   -1,   -1,  268,   -1,  315,   -1,   -1,  318,  319,
   -1,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,  329,
   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,
   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,   -1,
   -1,   -1,  352,  353,   -1,  310,   -1,   -1,   -1,   -1,
  315,  361,  362,  318,  319,   -1,  321,  322,  368,   -1,
   -1,   -1,   -1,  328,  329,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,   -1,   -1,   -1,  341,   -1,  343,  344,
  345,   -1,   -1,   -1,   -1,   -1,   -1,  352,  353,  258,
   -1,  260,  261,   -1,   -1,   -1,  361,  362,  267,  268,
  269,   -1,  271,  368,  273,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  283,  284,   -1,   -1,  287,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,
   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  338,
   -1,  340,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  349,   -1,  351,  352,  353,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,
  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  269,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  283,  405,   -1,   -1,   -1,
   -1,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  269,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  432,  279,  280,   -1,   -1,  283,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  449,  450,  451,  452,  453,  454,  455,  456,   -1,  458,
   -1,  460,  461,  462,  463,   -1,   -1,   -1,   -1,  314,
   -1,   -1,   -1,   -1,   -1,  320,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  498,
  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,  387,
   -1,  510,   -1,  391,  392,  393,  394,  395,  396,   -1,
  398,  399,  400,  401,  402,  403,  404,  405,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  258,   -1,  260,  261,   -1,   -1,  547,   -1,
   -1,   -1,  551,  552,  432,   -1,   -1,   -1,   -1,   -1,
  405,   -1,   -1,   -1,   -1,   -1,   -1,  283,   -1,   -1,
   -1,  449,  450,  451,  452,  453,  454,  455,  456,   -1,
  458,   -1,  460,  461,  462,  463,   -1,  432,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,
  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  547,
   -1,  387,   -1,  551,  552,  391,  392,  393,  394,  395,
  396,   -1,  398,  399,  400,  401,  402,  403,  404,  405,
   -1,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,
   -1,   -1,  547,  269,   -1,   -1,  551,  552,   -1,   -1,
   -1,   -1,   -1,  279,  280,   -1,  432,  283,   -1,   -1,
  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,  268,  458,   -1,  460,  461,  462,  463,  314,   -1,
   -1,   -1,   -1,  258,  320,  260,  261,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  269,   -1,   -1,   -1,  258,   -1,
  260,  261,   -1,   -1,  279,  280,   -1,   -1,  283,  269,
   -1,   -1,  498,  499,   -1,   -1,   -1,  503,  315,  505,
  506,   -1,   -1,  283,  321,  322,   -1,   -1,  325,   -1,
   -1,  328,   -1,   -1,   -1,   -1,   -1,  334,  335,  314,
   -1,   -1,   -1,   -1,  341,  320,   -1,   -1,   -1,   -1,
   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,   -1,
   -1,  547,   -1,  360,   -1,  551,  552,   -1,   -1,  405,
   -1,  379,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  392,  393,   -1,   -1,  396,   -1,
  398,   -1,   -1,   -1,   -1,   -1,  432,  405,   -1,  407,
  408,  409,  410,  411,  412,  413,  414,   -1,  416,  417,
  418,   -1,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,   -1,
  405,  258,   -1,  260,  261,   -1,  396,   -1,  398,   -1,
   -1,   -1,  269,   -1,   -1,  405,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  283,  432,   -1,   -1,
   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,  505,
  506,   -1,  432,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,  449,
  450,  451,  452,  453,  454,  455,  456,   -1,  458,   -1,
  460,  461,  462,  463,   -1,   -1,   -1,  258,   -1,  260,
  261,  547,   -1,   -1,   -1,  551,  552,   -1,  269,   -1,
   -1,   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,
  505,  506,  283,   -1,   -1,  432,  544,   -1,  498,  499,
   -1,   -1,  550,  503,   -1,  505,  506,   -1,   -1,   -1,
   -1,  378,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,   -1,  396,
   -1,  398,  547,   -1,   -1,   -1,  551,  552,  405,  258,
   -1,  260,  261,   -1,   -1,   -1,   -1,  547,   -1,   -1,
  269,  551,  552,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  283,  432,  503,   -1,  505,  506,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,  258,   -1,
  260,  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,
   -1,   -1,   -1,  550,  405,  552,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,
   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,   -1,   -1,  258,   -1,  260,  261,
  547,   -1,   -1,   -1,  551,  552,  405,  269,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  283,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,
   -1,   -1,  503,  432,  505,  506,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  519,   -1,
  449,  450,  451,  452,  453,  454,  455,  456,   -1,  458,
   -1,  460,  461,  462,  463,  405,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,   -1,  547,  269,   -1,   -1,
  551,  552,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  283,  432,   -1,   -1,   -1,   -1,   -1,   -1,  498,
  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,  449,
  450,  451,  452,  453,  454,  455,  456,   -1,  458,   -1,
  460,  461,  462,  463,   -1,  258,   -1,  260,  261,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,   -1,   -1,
   -1,   -1,   -1,  405,   -1,   -1,   -1,   -1,  547,   -1,
  283,   -1,  551,  552,   -1,   -1,   -1,   -1,  498,  499,
   -1,   -1,   -1,  503,   -1,  505,  506,   -1,   -1,   -1,
  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,   -1,  458,   -1,  460,  461,
  462,  463,   -1,  258,   -1,  260,  261,  547,   -1,   -1,
   -1,  551,  552,  405,  269,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  283,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,
  432,  503,   -1,  505,  506,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,   -1,  458,   -1,  460,  461,
  462,  463,  405,  258,   -1,  260,  261,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  269,  547,   -1,   -1,   -1,  551,
  552,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  283,  432,
   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,
   -1,  503,   -1,  505,  506,   -1,  449,  450,  451,  452,
  453,  454,  455,  456,   -1,  458,   -1,  460,  461,  462,
  463,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  405,  258,   -1,  260,  261,  547,   -1,   -1,   -1,  551,
  552,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,
  503,   -1,  505,  506,   -1,   -1,   -1,  432,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
   -1,   -1,   -1,   -1,  547,   -1,   -1,   -1,  551,  552,
  405,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  498,  499,   -1,   -1,  432,  503,   -1,
  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,  379,  458,   -1,  460,  461,  462,  463,   -1,
   -1,   -1,   -1,   -1,   -1,  392,  393,   -1,   -1,  396,
   -1,  398,  547,   -1,   -1,   -1,  551,  552,  405,   -1,
  407,  408,  409,  410,  411,  412,  413,  414,   -1,  416,
  417,  418,   -1,  498,  499,  258,   -1,  260,  503,   -1,
  505,  506,   -1,  266,  267,  268,  269,   -1,   -1,  272,
   -1,   -1,  275,   -1,   -1,   -1,  279,  280,   -1,  282,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,  547,  306,  307,   -1,  551,  552,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  279,   -1,   -1,  321,  322,
  323,  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,
   -1,  334,  335,   -1,   -1,  338,   -1,  340,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  349,   -1,  351,  352,
  353,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  364,   -1,  258,  367,  260,  369,   -1,   -1,   -1,
   -1,  266,  267,  268,  269,   -1,   -1,  544,   -1,   -1,
  275,   -1,   -1,  550,  279,  280,   -1,  282,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,  324,
   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  349,   -1,  351,  352,  353,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,
   -1,   -1,  367,  371,  369,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  446,  447,   -1,  449,  450,  451,  452,  453,
  454,  455,  456,  457,  458,   -1,   -1,  500,  501,  502,
  464,  465,  466,  467,  468,   -1,   -1,  471,  472,  407,
  474,  475,   -1,   -1,  412,   -1,   -1,   -1,  482,   -1,
  418,  485,  486,  487,  488,  489,  490,  491,  492,  493,
  494,  495,  496,  497,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  442,  443,   -1,   -1,  446,  447,
  448,  449,  450,  451,  452,  453,  454,  455,  456,  457,
  458,   -1,  460,  461,  462,  463,  464,  465,  466,  467,
  468,  469,  470,  471,  472,  473,  474,  475,  476,  477,
  258,   -1,  260,  481,   -1,   -1,   -1,   -1,   -1,  267,
  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  498,   -1,   -1,   -1,   -1,  500,  501,  502,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,
   -1,   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,
  338,   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,  260,
   -1,  349,   -1,  351,  352,  353,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,
   -1,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,
   -1,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,
   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,  349,   -1,
  351,  352,  353,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,
  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,
   -1,  258,   -1,  260,   -1,  349,   -1,  351,  352,  353,
  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  364,   -1,   -1,  367,   -1,  369,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,
   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,  306,
  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  321,  322,  323,  324,   -1,   -1,
  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,
   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,
  260,   -1,  349,   -1,  351,  352,  353,  267,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,
  367,   -1,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,
   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,
  340,   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,  349,
   -1,  351,  352,  353,  267,  268,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,   -1,   -1,   -1,   -1,  268,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,
  323,  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,
   -1,  334,  335,   -1,   -1,  338,   -1,  340,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  349,   -1,  351,  352,
  353,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,  364,  321,  322,  367,   -1,  369,   -1,   -1,  328,
  329,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,
   -1,   -1,  341,   -1,  343,  344,  345,  310,   -1,   -1,
   -1,   -1,  315,  352,  353,  318,  319,   -1,  321,  322,
   -1,   -1,  361,  362,   -1,  328,  329,   -1,   -1,  368,
   -1,  334,  335,   -1,   -1,   -1,   -1,   -1,  341,   -1,
  343,  344,  345,   -1,   -1,   -1,   -1,   -1,   -1,  352,
  353,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  361,  362,
   -1,   -1,   -1,   -1,   -1,  368,
  };

#line 3350 "D:/Development/Applications/mono2/mcs/ilasm/parser/ILParser.jay"

}

#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int EOF = 257;
  public const int ID = 258;
  public const int QSTRING = 259;
  public const int SQSTRING = 260;
  public const int COMP_NAME = 261;
  public const int INT32 = 262;
  public const int INT64 = 263;
  public const int FLOAT64 = 264;
  public const int HEXBYTE = 265;
  public const int DOT = 266;
  public const int OPEN_BRACE = 267;
  public const int CLOSE_BRACE = 268;
  public const int OPEN_BRACKET = 269;
  public const int CLOSE_BRACKET = 270;
  public const int OPEN_PARENS = 271;
  public const int CLOSE_PARENS = 272;
  public const int COMMA = 273;
  public const int COLON = 274;
  public const int DOUBLE_COLON = 275;
  public const int SEMICOLON = 277;
  public const int ASSIGN = 278;
  public const int STAR = 279;
  public const int AMPERSAND = 280;
  public const int PLUS = 281;
  public const int SLASH = 282;
  public const int BANG = 283;
  public const int ELLIPSIS = 284;
  public const int DASH = 286;
  public const int OPEN_ANGLE_BRACKET = 287;
  public const int CLOSE_ANGLE_BRACKET = 288;
  public const int UNKNOWN = 289;
  public const int INSTR_NONE = 290;
  public const int INSTR_VAR = 291;
  public const int INSTR_I = 292;
  public const int INSTR_I8 = 293;
  public const int INSTR_R = 294;
  public const int INSTR_BRTARGET = 295;
  public const int INSTR_METHOD = 296;
  public const int INSTR_NEWOBJ = 297;
  public const int INSTR_FIELD = 298;
  public const int INSTR_TYPE = 299;
  public const int INSTR_STRING = 300;
  public const int INSTR_SIG = 301;
  public const int INSTR_RVA = 302;
  public const int INSTR_TOK = 303;
  public const int INSTR_SWITCH = 304;
  public const int INSTR_PHI = 305;
  public const int INSTR_LOCAL = 306;
  public const int INSTR_PARAM = 307;
  public const int D_ADDON = 308;
  public const int D_ALGORITHM = 309;
  public const int D_ASSEMBLY = 310;
  public const int D_BACKING = 311;
  public const int D_BLOB = 312;
  public const int D_CAPABILITY = 313;
  public const int D_CCTOR = 314;
  public const int D_CLASS = 315;
  public const int D_COMTYPE = 316;
  public const int D_CONFIG = 317;
  public const int D_IMAGEBASE = 318;
  public const int D_CORFLAGS = 319;
  public const int D_CTOR = 320;
  public const int D_CUSTOM = 321;
  public const int D_DATA = 322;
  public const int D_EMITBYTE = 323;
  public const int D_ENTRYPOINT = 324;
  public const int D_EVENT = 325;
  public const int D_EXELOC = 326;
  public const int D_EXPORT = 327;
  public const int D_FIELD = 328;
  public const int D_FILE = 329;
  public const int D_FIRE = 330;
  public const int D_GET = 331;
  public const int D_HASH = 332;
  public const int D_IMPLICITCOM = 333;
  public const int D_LANGUAGE = 334;
  public const int D_LINE = 335;
  public const int D_XLINE = 336;
  public const int D_LOCALE = 337;
  public const int D_LOCALS = 338;
  public const int D_MANIFESTRES = 339;
  public const int D_MAXSTACK = 340;
  public const int D_METHOD = 341;
  public const int D_MIME = 342;
  public const int D_MODULE = 343;
  public const int D_MRESOURCE = 344;
  public const int D_NAMESPACE = 345;
  public const int D_ORIGINATOR = 346;
  public const int D_OS = 347;
  public const int D_OTHER = 348;
  public const int D_OVERRIDE = 349;
  public const int D_PACK = 350;
  public const int D_PARAM = 351;
  public const int D_PERMISSION = 352;
  public const int D_PERMISSIONSET = 353;
  public const int D_PROCESSOR = 354;
  public const int D_PROPERTY = 355;
  public const int D_PUBLICKEY = 356;
  public const int D_PUBLICKEYTOKEN = 357;
  public const int D_REMOVEON = 358;
  public const int D_SET = 359;
  public const int D_SIZE = 360;
  public const int D_STACKRESERVE = 361;
  public const int D_SUBSYSTEM = 362;
  public const int D_TITLE = 363;
  public const int D_TRY = 364;
  public const int D_VER = 365;
  public const int D_VTABLE = 366;
  public const int D_VTENTRY = 367;
  public const int D_VTFIXUP = 368;
  public const int D_ZEROINIT = 369;
  public const int K_AT = 370;
  public const int K_AS = 371;
  public const int K_IMPLICITCOM = 372;
  public const int K_IMPLICITRES = 373;
  public const int K_NOAPPDOMAIN = 374;
  public const int K_NOPROCESS = 375;
  public const int K_NOMACHINE = 376;
  public const int K_EXTERN = 377;
  public const int K_INSTANCE = 378;
  public const int K_EXPLICIT = 379;
  public const int K_DEFAULT = 380;
  public const int K_VARARG = 381;
  public const int K_UNMANAGED = 382;
  public const int K_CDECL = 383;
  public const int K_STDCALL = 384;
  public const int K_THISCALL = 385;
  public const int K_FASTCALL = 386;
  public const int K_MARSHAL = 387;
  public const int K_IN = 388;
  public const int K_OUT = 389;
  public const int K_OPT = 390;
  public const int K_STATIC = 391;
  public const int K_PUBLIC = 392;
  public const int K_PRIVATE = 393;
  public const int K_FAMILY = 394;
  public const int K_INITONLY = 395;
  public const int K_RTSPECIALNAME = 396;
  public const int K_STRICT = 397;
  public const int K_SPECIALNAME = 398;
  public const int K_ASSEMBLY = 399;
  public const int K_FAMANDASSEM = 400;
  public const int K_FAMORASSEM = 401;
  public const int K_PRIVATESCOPE = 402;
  public const int K_LITERAL = 403;
  public const int K_NOTSERIALIZED = 404;
  public const int K_VALUE = 405;
  public const int K_NOT_IN_GC_HEAP = 406;
  public const int K_INTERFACE = 407;
  public const int K_SEALED = 408;
  public const int K_ABSTRACT = 409;
  public const int K_AUTO = 410;
  public const int K_SEQUENTIAL = 411;
  public const int K_ANSI = 412;
  public const int K_UNICODE = 413;
  public const int K_AUTOCHAR = 414;
  public const int K_BESTFIT = 415;
  public const int K_IMPORT = 416;
  public const int K_SERIALIZABLE = 417;
  public const int K_NESTED = 418;
  public const int K_LATEINIT = 419;
  public const int K_EXTENDS = 420;
  public const int K_IMPLEMENTS = 421;
  public const int K_FINAL = 422;
  public const int K_VIRTUAL = 423;
  public const int K_HIDEBYSIG = 424;
  public const int K_NEWSLOT = 425;
  public const int K_UNMANAGEDEXP = 426;
  public const int K_PINVOKEIMPL = 427;
  public const int K_NOMANGLE = 428;
  public const int K_OLE = 429;
  public const int K_LASTERR = 430;
  public const int K_WINAPI = 431;
  public const int K_NATIVE = 432;
  public const int K_IL = 433;
  public const int K_CIL = 434;
  public const int K_OPTIL = 435;
  public const int K_MANAGED = 436;
  public const int K_FORWARDREF = 437;
  public const int K_RUNTIME = 438;
  public const int K_INTERNALCALL = 439;
  public const int K_SYNCHRONIZED = 440;
  public const int K_NOINLINING = 441;
  public const int K_CUSTOM = 442;
  public const int K_FIXED = 443;
  public const int K_SYSSTRING = 444;
  public const int K_ARRAY = 445;
  public const int K_VARIANT = 446;
  public const int K_CURRENCY = 447;
  public const int K_SYSCHAR = 448;
  public const int K_VOID = 449;
  public const int K_BOOL = 450;
  public const int K_INT8 = 451;
  public const int K_INT16 = 452;
  public const int K_INT32 = 453;
  public const int K_INT64 = 454;
  public const int K_FLOAT32 = 455;
  public const int K_FLOAT64 = 456;
  public const int K_ERROR = 457;
  public const int K_UNSIGNED = 458;
  public const int K_UINT = 459;
  public const int K_UINT8 = 460;
  public const int K_UINT16 = 461;
  public const int K_UINT32 = 462;
  public const int K_UINT64 = 463;
  public const int K_DECIMAL = 464;
  public const int K_DATE = 465;
  public const int K_BSTR = 466;
  public const int K_LPSTR = 467;
  public const int K_LPWSTR = 468;
  public const int K_LPTSTR = 469;
  public const int K_OBJECTREF = 470;
  public const int K_IUNKNOWN = 471;
  public const int K_IDISPATCH = 472;
  public const int K_STRUCT = 473;
  public const int K_SAFEARRAY = 474;
  public const int K_INT = 475;
  public const int K_BYVALSTR = 476;
  public const int K_TBSTR = 477;
  public const int K_LPVOID = 478;
  public const int K_ANY = 479;
  public const int K_FLOAT = 480;
  public const int K_LPSTRUCT = 481;
  public const int K_NULL = 482;
  public const int K_PTR = 483;
  public const int K_VECTOR = 484;
  public const int K_HRESULT = 485;
  public const int K_CARRAY = 486;
  public const int K_USERDEFINED = 487;
  public const int K_RECORD = 488;
  public const int K_FILETIME = 489;
  public const int K_BLOB = 490;
  public const int K_STREAM = 491;
  public const int K_STORAGE = 492;
  public const int K_STREAMED_OBJECT = 493;
  public const int K_STORED_OBJECT = 494;
  public const int K_BLOB_OBJECT = 495;
  public const int K_CF = 496;
  public const int K_CLSID = 497;
  public const int K_METHOD = 498;
  public const int K_CLASS = 499;
  public const int K_PINNED = 500;
  public const int K_MODREQ = 501;
  public const int K_MODOPT = 502;
  public const int K_TYPEDREF = 503;
  public const int K_TYPE = 504;
  public const int K_WCHAR = 505;
  public const int K_CHAR = 506;
  public const int K_FROMUNMANAGED = 507;
  public const int K_CALLMOSTDERIVED = 508;
  public const int K_BYTEARRAY = 509;
  public const int K_WITH = 510;
  public const int K_INIT = 511;
  public const int K_TO = 512;
  public const int K_CATCH = 513;
  public const int K_FILTER = 514;
  public const int K_FINALLY = 515;
  public const int K_FAULT = 516;
  public const int K_HANDLER = 517;
  public const int K_TLS = 518;
  public const int K_FIELD = 519;
  public const int K_PROPERTY = 520;
  public const int K_REQUEST = 521;
  public const int K_DEMAND = 522;
  public const int K_ASSERT = 523;
  public const int K_DENY = 524;
  public const int K_PERMITONLY = 525;
  public const int K_LINKCHECK = 526;
  public const int K_INHERITCHECK = 527;
  public const int K_REQMIN = 528;
  public const int K_REQOPT = 529;
  public const int K_REQREFUSE = 530;
  public const int K_PREJITGRANT = 531;
  public const int K_PREJITDENY = 532;
  public const int K_NONCASDEMAND = 533;
  public const int K_NONCASLINKDEMAND = 534;
  public const int K_NONCASINHERITANCE = 535;
  public const int K_READONLY = 536;
  public const int K_NOMETADATA = 537;
  public const int K_ALGORITHM = 538;
  public const int K_FULLORIGIN = 539;
  public const int K_ENABLEJITTRACKING = 540;
  public const int K_DISABLEJITOPTIMIZER = 541;
  public const int K_RETARGETABLE = 542;
  public const int K_PRESERVESIG = 543;
  public const int K_BEFOREFIELDINIT = 544;
  public const int K_ALIGNMENT = 545;
  public const int K_NULLREF = 546;
  public const int K_VALUETYPE = 547;
  public const int K_COMPILERCONTROLLED = 548;
  public const int K_REQSECOBJ = 549;
  public const int K_ENUM = 550;
  public const int K_OBJECT = 551;
  public const int K_STRING = 552;
  public const int K_TRUE = 553;
  public const int K_FALSE = 554;
  public const int K_IS = 555;
  public const int K_ON = 556;
  public const int K_OFF = 557;
  public const int K_FORWARDER = 558;
  public const int K_CHARMAPERROR = 559;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }
  internal class yyUnexpectedEof : yyException {
    public yyUnexpectedEof (string message) : base (message) {
    }
    public yyUnexpectedEof () : base ("") {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
