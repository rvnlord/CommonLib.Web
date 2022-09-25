using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CommonLib.Source.Common.Extensions;
using CommonLib.Source.Common.Extensions.Collections;
using CommonLib.Source.Common.Utils.UtilClasses;
using Microsoft.AspNetCore.Components.Forms;

namespace CommonLib.Web.Source.Common.Utils.UtilClasses
{
    public sealed class MyValidationMessageStore
    {
        private readonly MyEditContext _editContext;
        private readonly ConcurrentDictionary<FieldIdentifier, List<string>> _messages = new();

        public MyValidationMessageStore(MyEditContext editContext)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
        }

        public void Add(in FieldIdentifier fieldIdentifier, string message) => GetOrCreateMessagesListForField(fieldIdentifier).Add(message);
        public void Add(Expression<Func<object>> accessor, string message) => Add(FieldIdentifier.Create(accessor), message);
        public void Add(in FieldIdentifier fieldIdentifier, IEnumerable<string> messages) => GetOrCreateMessagesListForField(fieldIdentifier).AddRange(messages);
        public void Add(Expression<Func<object>> accessor, IEnumerable<string> messages) => Add(FieldIdentifier.Create(accessor), messages);
        public IEnumerable<string> this[FieldIdentifier fieldIdentifier] => _messages.TryGetValue(fieldIdentifier, out var messages) ? messages : Enumerable.Empty<string>();
        public IEnumerable<string> this[Expression<Func<object>> accessor] => this[FieldIdentifier.Create(accessor)];

        public void Clear()
        {
            foreach (var fieldIdentifier in _messages.Keys)
                DissociateFromField(fieldIdentifier);

            _messages.Clear();
        }

        public void Clear(Expression<Func<object>> accessor) => Clear(FieldIdentifier.Create(accessor));

        public void Clear(IEnumerable<FieldIdentifier> fieldIdentifiers)
        {
            foreach (var fieldIdentifier in fieldIdentifiers)
            {
                DissociateFromField(fieldIdentifier);
                _messages.Remove(fieldIdentifier, out _);
            }
        }

        public void Clear(in FieldIdentifier fieldIdentifier)
        {
            DissociateFromField(fieldIdentifier);
            _messages.Remove(fieldIdentifier, out _);
        }

        public List<string> GetOrCreateMessagesListForField(in FieldIdentifier fieldIdentifier)
        {
            if (!_messages.TryGetValue(fieldIdentifier, out var messagesForField))
            {
                messagesForField = new List<string>();
                _messages[fieldIdentifier] = messagesForField;
                AssociateWithField(fieldIdentifier);
            }

            return messagesForField;
        }

        private void AssociateWithField(in FieldIdentifier fieldIdentifier) => _editContext.GetFieldState(fieldIdentifier, true).AssociateWithValidationMessageStore(this);
        private void DissociateFromField(in FieldIdentifier fieldIdentifier) => _editContext.GetFieldState(fieldIdentifier, false)?.DissociateFromValidationMessageStore(this);

        public bool HasNoMessages() => !_messages.Where(kvp => kvp.Value != null).SelectMany(kvp => kvp.Value).Any(); // it should say its empty when there are no validation messages, even if there are empty keys in the dictionary, this way we can know when there is successful validation for a field (empty field identifier without messages)
        public bool HasNoMessages(FieldIdentifier fi) => _messages.VorN(fi) == null || !_messages.VorN(fi).Any();
        public bool HasNoMessages(IEnumerable<FieldIdentifier> fis) => !_messages.Where(kvp => kvp.Value != null && kvp.Key.In(fis)).SelectMany(kvp => kvp.Value).Any();
        public List<FieldIdentifier> GetInvalidFields() => _messages.Where(fi => fi.Value.Any()).Select(kvp => kvp.Key).ToList();

        public bool WasValidated(FieldIdentifier fi) => _messages.VorN(fi) != null;
        public bool WasValidated<TProperty>(Expression<Func<TProperty>> accessor)
        {
            var (m, p, _, _) = accessor.GetModelAndProperty();
            return WasValidated(new FieldIdentifier(m, p));
        }
    }
}
