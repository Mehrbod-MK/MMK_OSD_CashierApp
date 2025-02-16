﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class InputForm_ViewModel : ViewModelBase
    {
        /// <summary>
        /// Represents a custom for field for InputForm.
        /// </summary>
        public class Struct_FormField : ViewModelBase
        {
            private string question = string.Empty;
            private string? defaultResponse = null;
            
            public string Question
            {
                get => question;
                set => SetProperty(ref question, value);
            }

            public string? DefaultResponse
            {
                get => defaultResponse;
                set => SetProperty(ref defaultResponse, value);
            }
        }

        private ObservableCollection<Struct_FormField> formFields;
        public ObservableCollection<Struct_FormField> FormFields
        {
            get => formFields;
            set => SetProperty(ref formFields, value);
        }

        private string formTitle;
        public string FormTitle
        {
            get => formTitle;
            set => SetProperty(ref formTitle, value);
        }

        private Func<bool> function_EvaluateFields;
        public Func<bool> Function_EvaluateFields => function_EvaluateFields;

        public bool IsEvaluationOK { get; set; }

        private Window? window_InputForm;
        public Window? Window_InputForm
        {
            get => window_InputForm;
            set => SetProperty(ref window_InputForm, value);
        }

        public InputForm_ViewModel(string formTitle, List<Struct_FormField> formFields)
        {
            this.formTitle = formTitle;
            this.formFields = new ObservableCollection<Struct_FormField>(formFields);

            this.function_EvaluateFields = bool () => true;
        }

        public List<Struct_FormField>? DisplayForm(Func<bool> evaluationFunction)
        {
            if (this.Window_InputForm == null)
                return null;

            this.function_EvaluateFields = evaluationFunction;

            Window_InputForm.ShowDialog();

            if (function_EvaluateFields.Invoke() == true)
                return FormFields.ToList();
            else
                return null;
        }
    }
}
