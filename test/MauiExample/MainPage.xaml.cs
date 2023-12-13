using Petstore;

namespace MauiExample;

public partial class MainPage : ContentPage
{
	int count = 0;
	ISwaggerPetstore petstore;

	public MainPage(ISwaggerPetstore petstore)
	{
		InitializeComponent();
		this.petstore = petstore;
	}

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		try
		{
			var response = await petstore.GetPetById(count);
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				CounterBtn.Text = $"Error: {response.StatusCode}";
			else
				CounterBtn.Text = response.Content.Name;
		}
		catch (Refit.ApiException ex)
		{
			CounterBtn.Text = "Refit call failed: " + ex.Message;			
		}

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}

