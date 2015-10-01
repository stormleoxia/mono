using System;
using System.Web.Util;
namespace System.Web.Caching
{
	[Serializable]
	internal class CachedVary
	{
		private Guid _cachedVaryId;
		internal readonly string[] _contentEncodings;
		internal readonly string[] _headers;
		internal readonly string[] _params;
		internal readonly string _varyByCustom;
		internal readonly bool _varyByAllParams;
		internal Guid CachedVaryId
		{
			get
			{
				return this._cachedVaryId;
			}
		}
		internal CachedVary(string[] contentEncodings, string[] headers, string[] parameters, bool varyByAllParams, string varyByCustom)
		{
			this._contentEncodings = contentEncodings;
			this._headers = headers;
			this._params = parameters;
			this._varyByAllParams = varyByAllParams;
			this._varyByCustom = varyByCustom;
			this._cachedVaryId = Guid.NewGuid();
		}
		public override bool Equals(object obj)
		{
			CachedVary cachedVary = obj as CachedVary;
			return cachedVary != null && (this._varyByAllParams == cachedVary._varyByAllParams && this._varyByCustom == cachedVary._varyByCustom && StringUtil.StringArrayEquals(this._contentEncodings, cachedVary._contentEncodings) && StringUtil.StringArrayEquals(this._headers, cachedVary._headers)) && StringUtil.StringArrayEquals(this._params, cachedVary._params);
		}
		public override int GetHashCode()
		{
			HashCodeCombiner expr_05 = new HashCodeCombiner();
			expr_05.AddObject(this._varyByAllParams);
			expr_05.AddObject(this._varyByCustom);
			expr_05.AddArray(this._contentEncodings);
			expr_05.AddArray(this._headers);
			expr_05.AddArray(this._params);
			return expr_05.CombinedHash32;
		}
	}
}
