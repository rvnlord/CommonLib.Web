﻿using System;
using System.Collections.Generic;

namespace CommonLib.Web.Source.ViewModels.Account
{
    public class FindRoleVM
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<string> UserNames { get; set; } = new();

        public override bool Equals(object o)
        {
            if (o is not FindRoleVM role)
                return false;

            return Name == role.Name;
        }

        public override int GetHashCode() => Name.GetHashCode() ^ 3 * Id.GetHashCode() ^ 5 * UserNames.GetHashCode() ^ 7;

        public override string ToString() => Name;
    }
}
