﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MileEyes.Services.Models;
using Xamarin.Forms;

namespace MileEyes.ViewModels
{
    class CompaniesViewModel : ViewModel
    {
        public ObservableCollection<CompanyViewModel> Companies { get; set; } = new ObservableCollection<CompanyViewModel>();

        private CompanyViewModel _selectedCompany;

        public CompanyViewModel SelectedCompany
        {
            get { return _selectedCompany; }
            set
            {
                _selectedCompany = value;
                OnPropertyChanged(nameof(SelectedCompany));
            }
        }

        public CompaniesViewModel()
        {
            SelectCommand = new Command(Select);
            Refresh();
        }

        public override async void Refresh()
        {
            base.Refresh();

            Refreshing = true;

            Companies.Clear();
            var companies = await Services.Host.CompanyService.GetCompanies();

            var c1 = companies;

            foreach (var c in companies)
            {
                Companies.Add(new CompanyViewModel(c));
            }

            Refreshing = false;
        }

        public ICommand SelectCommand { get; set; }

        public event EventHandler<CompanyViewModel> Selected = delegate { };

        public event EventHandler NotSelected = delegate { };

        public void Select()
        {
            if (SelectedCompany != null)
            {
                Selected?.Invoke(this, SelectedCompany);
            }
            else
            {
                NotSelected?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
