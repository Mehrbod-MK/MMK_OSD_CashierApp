﻿using MMK_OSD_CashierApp.Helpers;
using MMK_OSD_CashierApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MMK_OSD_CashierApp.ViewModels
{
    public class ProductManager_ViewModel : ViewModelBase
    {
        private ObservableCollection<Product> queriedProducts;
        public ObservableCollection<Product> QueriedProducts
        {
            get => queriedProducts;
            set => SetProperty(ref queriedProducts, value);
        }

        private string? search_ProductID;
        public string? Search_ProductID
        {
            get => search_ProductID;
            set => SetProperty(ref search_ProductID, value);
        }

        private string? search_ProductName;
        public string? Search_ProductName
        {
            get => search_ProductName;
            set => SetProperty(ref search_ProductName, value);
        }

        private string? search_ProductPrice;
        public string? Search_ProductPrice
        {
            get => search_ProductPrice;
            set => SetProperty(ref search_ProductPrice, value);
        }

        private string? search_ProductVendor;
        public string? Search_ProductVendor
        {
            get => search_ProductVendor;
            set => SetProperty(ref search_ProductVendor, value);
        }

        private bool? contains_ProductID = false;
        public bool? Contains_ProductID
        {
            get => contains_ProductID;
            set => SetProperty(ref contains_ProductID, value);
        }

        private bool? contains_ProductName = false;
        public bool? Contains_ProductName
        {
            get => contains_ProductName;
            set => SetProperty(ref contains_ProductName, value);
        }

        private bool? minimum_ProductPrice = false;
        public bool? Minimum_ProductPrice
        {
            get => minimum_ProductPrice;
            set => SetProperty(ref minimum_ProductPrice, value);
        }

        private bool? maximum_ProductPrice = false;
        public bool? Maximum_ProductPrice
        {
            get => maximum_ProductPrice;
            set => SetProperty(ref maximum_ProductPrice, value);
        }

        private bool? contains_ProductVendor = false;
        public bool? Contains_ProductVendor
        {
            get => contains_ProductVendor;
            set => SetProperty(ref contains_ProductVendor, value);
        }

        private bool? display_Unavailables = false;
        public bool? Display_Unavailables
        {
            get => display_Unavailables;
            set => SetProperty(ref display_Unavailables, value);
        }

        private bool? display_AboutToRunOuts = false;
        public bool? Display_AboutToRunOuts
        {
            get => display_AboutToRunOuts;
            set => SetProperty(ref display_AboutToRunOuts, value);
        }

        private bool? display_NoPhotos = false;
        public bool? Display_NoPhotos
        {
            get => display_NoPhotos;
            set => SetProperty(ref display_NoPhotos, value);
        }

        private RelayCommand command_SearchProducts;
        public ICommand Command_SearchProducts => command_SearchProducts;
        public void Order_SearchProducts(object? parameter)
        {
            if (parameter is not Window wnd_ProductManager)
                return;

            Worker_ViewModel vm_Worker = new();

            BackgroundWorker worker_SearchProducts = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };

            worker_SearchProducts.DoWork += (sender, e) =>
            {
                try
                {
                    vm_Worker.ProgressState = "در حال ساخت کوئری...";

                    string productsSearchQuery = Generate_SearchProductConditionalQuery();

                    vm_Worker.ProgressState = "در حال جستجوی پایگاه داده...";

                    var resultOfSearch = DB._THROW_DBRESULT<List<Product>>(
                        MainWindow.db.db_Get_Products(productsSearchQuery).Result
                        );

                    if (resultOfSearch == null)
                        throw new NullReferenceException("پاسخی از سوی پایگاه داده دریافت نشد.");

                    if (resultOfSearch.Count == 0)
                    {
                        MakeMessageBoxes.Display_Notification(
                            "جستجو نتیجه‌ای در بر نداشت.",
                            "",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxResult.OK
                            );

                        return;
                    }

                    // Application.Current.Dispatcher.Invoke(() => QueriedProducts = new ObservableCollection<Product>(resultOfSearch));
                    QueriedProducts = new ObservableCollection<Product>(resultOfSearch);
                }
                catch (Exception ex)
                {
                    MakeMessageBoxes.Display_Error_DB(ex);
                }
            };

            wnd_ProductManager.IsEnabled = false;
            Dialog_Worker workerDlg = new(worker_SearchProducts, vm_Worker);
            workerDlg.ShowDialog();
            wnd_ProductManager.IsEnabled = true;
        }
        public bool Allow_SearchProducts(object? parameter)
        {
            return true;
        }

        public ProductManager_ViewModel(User loggedIn_User)
        {
            queriedProducts = new ObservableCollection<Product>();

            command_SearchProducts = new RelayCommand(Order_SearchProducts, Allow_SearchProducts);
        }

        public string Generate_SearchProductConditionalQuery()
        {
            string searchQuery = $"SELECT * FROM {DB.DB_TABLE_NAME_PRODUCTS} WHERE ";

            if (!string.IsNullOrEmpty(search_ProductID) && uint.TryParse(search_ProductID, out uint prodID))
            {
                if (contains_ProductID == true)
                {
                    searchQuery += $"ProductID LIKE \'%{prodID}%\' AND ";
                }
                else
                {
                    searchQuery += $"ProductID = {prodID} AND ";
                }
            }

            if (!string.IsNullOrEmpty(search_ProductName))
            {
                if (contains_ProductName == true)
                {
                    searchQuery += $"ProductName LIKE \'%{search_ProductName}%\' AND ";
                }
                else
                {
                    searchQuery += $"ProductName = '{search_ProductName}' AND ";
                }
            }

            if (!string.IsNullOrEmpty(search_ProductPrice) && uint.TryParse(search_ProductPrice, out uint price))
            {
                if (minimum_ProductPrice == true)
                {
                    searchQuery += $"Price >= \'%{price}%\' AND ";
                }
                if (maximum_ProductPrice == true)
                {
                    searchQuery += $"Price <= {price} AND ";
                }
            }

            if (!string.IsNullOrEmpty(search_ProductVendor))
            {
                if (contains_ProductVendor == true)
                {
                    searchQuery += $"Vendor LIKE \'%{search_ProductVendor}%\' AND ";
                }
                else
                {
                    searchQuery += $"Vendor = '{search_ProductVendor}' AND ";
                }
            }

            if (display_Unavailables == true)
            {
                searchQuery += "Quantity = 0 AND ";
            }

            if (display_AboutToRunOuts == true)
            {
                searchQuery += "Quantity > 0 AND Quantity <= 5 AND ";
            }

            if (display_NoPhotos == true)
            {
                searchQuery += "ThumbImagePath = NULL AND ";
            }

            // End search query for AND operators.
            searchQuery += "TRUE;";

            return searchQuery;
        }
    }
}
