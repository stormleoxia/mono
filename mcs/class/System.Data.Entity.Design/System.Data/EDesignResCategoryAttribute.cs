using System;
using System.ComponentModel;

namespace System.Data
{
	[AttributeUsage(AttributeTargets.All)]
	internal sealed class EDesignResCategoryAttribute : CategoryAttribute
	{
		public EDesignResCategoryAttribute(string category) : base(category)
		{
		}

		protected override string GetLocalizedString(string value)
		{
			return EDesignRes.GetString(value);
		}
	}
}
