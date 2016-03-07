using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    internal class DynamicRoleClaimProvider
    {
        private static readonly PropertyInfo property;

        static DynamicRoleClaimProvider()
        {
            property = typeof (ClaimsIdentity).GetProperty("ExternalClaims", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ClaimsAuthenticationManager to add claims to a ClaimsIdentity", true)]
        public static void AddDynamicRoleClaims(ClaimsIdentity claimsIdentity, IEnumerable<Claim> claims)
        {
            Collection<IEnumerable<Claim>> externalClaims = property.GetValue(claimsIdentity) as Collection<IEnumerable<Claim>>;
            if (externalClaims != null)
            {
                externalClaims.Add(claims);
            }
        }
    }
}
