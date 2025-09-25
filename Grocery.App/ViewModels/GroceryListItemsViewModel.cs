using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;
        private string searchText = "";

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];
        public ObservableCollection<GroceryListItem> FilteredGroceryListItems { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);
        [ObservableProperty]
        string myMessage;
        [ObservableProperty]
        string searchBarText = string.Empty;
        [ObservableProperty]
        string cartSearchBarText = string.Empty;

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
            GetFilteredGroceryListItems();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            var filter = string.IsNullOrWhiteSpace(SearchBarText) ? string.Empty : SearchBarText.ToLower();

            var filteredProducts = _productService.GetAll()
                .Where(p =>
                    MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null &&
                    p.Stock > 0 &&
                    (string.IsNullOrEmpty(filter) || p.Name.ToLower().Contains(filter)))
                .ToList();

            foreach (var p in filteredProducts)
                AvailableProducts.Add(p);
        }

        private void GetFilteredGroceryListItems()
        {
            FilteredGroceryListItems.Clear();
            var filter = string.IsNullOrWhiteSpace(CartSearchBarText) ? string.Empty : CartSearchBarText.ToLower();
            var filteredItems = MyGroceryListItems
                .Where(item => string.IsNullOrEmpty(filter) || (item.Product != null && item.Product.Name.ToLower().Contains(filter)))
                .ToList();
            foreach (var item in filteredItems)
                FilteredGroceryListItems.Add(item);
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);
            AvailableProducts.Remove(product);
            OnGroceryListChanged(GroceryList);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

        [RelayCommand]
        public void PerformSearch(string searchText)
        {
            SearchBarText = searchText;
            GetAvailableProducts();
        }

        [RelayCommand]
        public void PerformCartSearch(string searchText)
        {
            CartSearchBarText = searchText;
            GetFilteredGroceryListItems();
        }
    }
}
