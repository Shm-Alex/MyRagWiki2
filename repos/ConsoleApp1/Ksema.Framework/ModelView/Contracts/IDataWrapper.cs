using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace Ksema.Framework.ModelView
{
    /// <summary>
    /// Контэйнер ошибок - DataWrapper или ValidationWrapper
    /// </summary>
    public interface IValidationErrorContainer
    {
        IEnumerable<ValidationError> Errors { get; }
        object Uri { get; set; }
    }

    /// <summary>
    /// Что-то вроде IDataWrapper, но цепляется к контролу через аттачед свойство
    /// и по Trigger проводит валидацию
    /// ошибки цепляет на контрол
    /// </summary>
    public interface IValidationWrapper : INotifyPropertyChanged, IValidationErrorContainer
    {
        string Id { get; }
        IObservable<Unit> Trigger { get; set; }
        void Validate();

        void AddRule(Func<ValidationResult> validator);
		void AddRule(ValidationRule rule);

        ReadOnlyObservableCollection<ValidationRule> Rules { get; }

        void OnBeforeAttach(Binding binding, DependencyObject target, DependencyProperty property);
        void OnAfterAttach(BindingExpressionBase bindingExpression);
    }

    public interface IDataWrapper : IAccessUnit, IValidationErrorContainer
    {
        string Id { get; }

        bool IsAttached { get; }

        void AddRule(Func<ValidationResult> validator);
		void AddRule(ValidationRule rule);
        void AddAsyncRule(Func<CancellationToken, ValidationResult> validator);

        ObservableCollection<IDataWrapper> Dependent { get; }
        ObservableCollection<IDataWrapper> DependsOn { get; }

        Func<object, object> Convert { get; set; }
        Func<object, object> ConvertBack { get; set; }

        bool IsValid { get; }

        // Запустить валидацию
        Task Validate();
        void UpdateSource();

        /// <summary>
        /// Родительский враппер
        /// Для враппера Index.Measure.Name, родителем будет Index.Measure
        /// родительские врапперы создаются автоматически в случае если их ещё нет
        /// </summary>
        IDataWrapper Parent { get; set; }

        /// <summary>
        /// Все дочерние врапперы на один уровень
        /// </summary>
        ObservableCollection<IDataWrapper> Children { get; }

        ReadOnlyObservableCollection<ValidationRule> Rules { get; }

        void OnBeforeAttach(Binding binding, DependencyObject target, DependencyProperty property);
        void OnAfterAttach(BindingExpressionBase bindingExpression);
    }
}
