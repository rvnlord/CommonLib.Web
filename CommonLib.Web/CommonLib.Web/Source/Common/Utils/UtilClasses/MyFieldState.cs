using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public class MyFieldState
    {
        private readonly FieldIdentifier _fieldIdentifier;
        private HashSet<MyValidationMessageStore> _validationMessageStores;

        public MyFieldState(FieldIdentifier fieldIdentifier)
        {
            _fieldIdentifier = fieldIdentifier;
        }

        public bool IsModified { get; set; }

        public IEnumerable<string> GetValidationMessages()
        {
            if (_validationMessageStores == null) 
                yield break;

            foreach (var message in _validationMessageStores.SelectMany(store => store[_fieldIdentifier]))
                yield return message;
        }

        public void AssociateWithValidationMessageStore(MyValidationMessageStore validationMessageStore)
        {
            if (_validationMessageStores == null)
                _validationMessageStores = new HashSet<MyValidationMessageStore>();

            _validationMessageStores.Add(validationMessageStore);
        }

        public void DissociateFromValidationMessageStore(MyValidationMessageStore validationMessageStore) => _validationMessageStores?.Remove(validationMessageStore);
    }
}
