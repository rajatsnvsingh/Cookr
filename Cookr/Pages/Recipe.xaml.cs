﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cookr
{
	enum RecipeUserVisible { invisible, partial, full, };

	// ANDY - All the changes you need to make would be on this page.
	public partial class Recipe : Page
	{
		public RecipeObject recipe;

		// List of the buttons making up that list on the left
		public List<RecipeStepButtonCustom> stepButtons;

		// List of the elements in the recipe's RecipeStepStack that can be used for navigation.
		public List<UIElement> recipeScrollableNavigationThingys;

		public Recipe()
		{
			InitializeComponent();
		}

		public void SetRecipe(RecipeObject _recipe)
		{
			recipe = _recipe;

			// Populate Title, time, star rating, TitleImage.
			RecipeTitle.Text = recipe.Title;
            TimeSpan total = TimeSpan.FromMinutes(_recipe.TotalTime);
            string recipetime = total.ToString("g");
            RecipeTime.Text = recipetime.Substring(0, recipetime.LastIndexOf(':'));
            //RecipeStarRating.Value = (int)(recipe.Rating);

            // Populate the star rating
            PopulateStarRating((int)recipe.Rating);

			RecipeTitleImage.ImageSource = new BitmapImage(
										   new Uri("Images/" + recipe.TitleImage, UriKind.Relative));

			// Populate recipe introduction, ingredients, and tools.
			DescriptionTextBlock.Text = recipe.RecipeIntroduction + "\r\rPrep:\t"
										+ hoursAndMinutesString(recipe.PrepTime) + "\rCook:\t"
										+ hoursAndMinutesString(recipe.CookTime) + "\rServings: "
										+ recipe.NumberOfServings.ToString();

			GenerateIngredientsList(IngredientsTextBlock);
			GenerateToolsList(ToolsTextBlock);

			LoadRecipeSteps();

			fillStepButtonsList();

			SetUpButtonDefaults();
			fillRecipeScrollableNavigationThingyList();

            RecipeStepScrollViewer.ScrollToHome();

        }

        private string hoursAndMinutesString(int time)
        {
            int hours = time / 60;
            string result = "";
            if (hours > 0)
            {
                result = hours.ToString() + " hours ";
            }
            if (time % 60 > 0)
            {
                result += (time % 60).ToString() + " minutes";
            }
            return result;
        }

        /// <summary>
        /// Go ham on that star rating.
        /// </summary>
        /// <param name="rating"></param>
        private void PopulateStarRating(int rating)
        {
            // Put all the stars in a list
            List<TextBlock> stars = new List<TextBlock>() { Star1, Star2, Star3, Star4, Star5 };
            for(int i = 0; i < 5; i++)
            {
                if(rating > i)
                {
                    stars[i].Text = (string)FindResource("Star-Icon");
                    stars[i].Foreground = (SolidColorBrush)FindResource("Star-Brush");
                }
                else
                {
                    stars[i].Text = (string)FindResource("Star-Empty-Icon");
                    stars[i].Foreground = (SolidColorBrush)FindResource("Inactive-Light-Brush");
                }
            }
        }


        /// <summary>
        /// Set up the first 3 buttons for the recipe more directly.
        /// Also set up a callback function for when the buttons are clicked.
        /// </summary>
        private void SetUpButtonDefaults()
		{
			stepButtons[0].Content.Text = recipe.Title;
			stepButtons[1].Content.Text = "Ingredients";
			stepButtons[2].Content.Text = "Tools";
			stepButtons[0].Content.HorizontalAlignment = HorizontalAlignment.Center;
			stepButtons[1].Content.HorizontalAlignment = HorizontalAlignment.Center;
			stepButtons[2].Content.HorizontalAlignment = HorizontalAlignment.Center;

			foreach (RecipeStepButtonCustom b in stepButtons)
			{
				b.Listener = NavigationButton_Click;
			}
		}

		private void NavigationButton_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < stepButtons.Count; i++)
			{
				if ((RecipeStepButtonCustom)sender == stepButtons[i])
				{
					//Navigate to the corresponding UI element In the recipe stackpanel
					UIElement item = recipeScrollableNavigationThingys[i];
					Rect bounds = item.TransformToAncestor(RecipeStepScrollViewer).TransformBounds(new Rect(0.0, 0.0, item.RenderSize.Width, item.RenderSize.Height));
					RecipeStepScrollViewer.ScrollToVerticalOffset(bounds.Y + RecipeStepScrollViewer.ContentVerticalOffset);
				}
			}
		}

		/// <summary>
		/// An absolute hack way to get all the navigation buttons in a list, where we can do whatever
		/// We want to em. This might not be the best way to get around the fact that 2 of those boys
		/// are mushed together in a grid but this works.
		/// </summary>
		private void fillRecipeScrollableNavigationThingyList()
		{
			recipeScrollableNavigationThingys = new List<UIElement>();
			// Add the pieces we already know are going to be there directly
			recipeScrollableNavigationThingys.Add(RecipeTitleImageGrid);
			recipeScrollableNavigationThingys.Add(IngredientsTextBlock);
			recipeScrollableNavigationThingys.Add(ToolsTextBlock);

			// And then make sure we don't add them again...
			// We're skipping the title image, description, ingredients, tools.
			int elementsToSkip = 4;
			foreach (UIElement e in RecipeStepStack.Children)
			{
				if (elementsToSkip > 0)
				{
					elementsToSkip--;
					continue;
				}
				recipeScrollableNavigationThingys.Add(e);
			}

			// Remove the last two elements we added since they're the little bar that says done and the rating section...
			recipeScrollableNavigationThingys.RemoveRange(recipeScrollableNavigationThingys.Count - 2, 2);

		}

		private void fillStepButtonsList()
		{
			stepButtons = new List<RecipeStepButtonCustom>();
			foreach (UIElement e in RecipeButtonStack.Children)
			{
				if (e.GetType() == typeof(Grid))
				{
					Grid grid = (Grid)e;
					foreach (UIElement ge in grid.Children)
					{
						stepButtons.Add((RecipeStepButtonCustom)ge);
					}
				}
				else
				{
					stepButtons.Add((RecipeStepButtonCustom)e);
				}
			}
		}

		private void LoadRecipeSteps()
		{

            // Clear out old Recipe buttons
            while (RecipeButtonStack.Children.Count > 2)
            {
                RecipeButtonStack.Children.RemoveAt(RecipeButtonStack.Children.Count - 1);
            }
            while (RecipeStepStack.Children.Count > 4)
            {
                RecipeStepStack.Children.RemoveAt(RecipeStepStack.Children.Count - 1);
            }

            // Use the list of RecipeSteps to populate the list of buttons and recipe steps

            // Keep track of the current step type so we know when to add in the little extra bars
            // That say Cook or Prep or whatever.
            string currentType = "";
			foreach (RecipeStep rs in recipe.RecipeSteps)
			{
				if (!rs.Type.Equals(currentType))
				{
					// We changed to a different phase/type of step, so add in some little bar guys.
					currentType = rs.Type;
					RecipeButtonStack.Children.Add(new RecipeStepButtonCustom(currentType, true));
					RecipeStepStack.Children.Add(new RecipePhase(currentType));
				}

				// Add the for realsies button for this step
				RecipeButtonStack.Children.Add(new RecipeStepButtonCustom(rs.Number.ToString() + ". " + rs.Title, false));

				// Add the for realsies step... for this step.
				RecipeStepStack.Children.Add(new RecipeStepLayout(rs, this));
			}

			// Finally, add little "All done!" strip and recipe rating section
			RecipeStepStack.Children.Add(new RecipePhase("All done!"));

			// Add the rating bar at the bottom. populate the already voted value.
			RecipeRatingStepLayout rateStep = new RecipeRatingStepLayout();
			rateStep.listener = RecipeRatingUpdated;
			rateStep.Value = recipe.UserRating;
			RecipeStepStack.Children.Add(rateStep);

		}

		public void RecipeRatingUpdated(int rating)
		{
			recipe.UserRating = rating;
		}

		// Uses the recipe object to parse the ingredients list into something that's nice to display
		private void GenerateIngredientsList(TextBlock textblock)
		{
			textblock.Inlines.Clear();
			textblock.Inlines.Add("Ingredients:\r\n\r\n");
			foreach (Ingredient i in recipe.Ingredients)
			{
				textblock.Inlines.Add("- " + i.Quanitity + " ");
				if (i.ToolTipID != 0)
				{
					textblock.Inlines.Add(CreateInTextToolTip(i.Name, i.ToolTipID));
				}
				else
				{
					textblock.Inlines.Add(i.Name);
				}
				if (i.Optional)
				{
					textblock.Inlines.Add(" (Optional)");
				}
				textblock.Inlines.Add("\r\n");
			}

		}

		// Uses the recipe object to parse the tools list into something that's nice to display
		private void GenerateToolsList(TextBlock textblock)
		{
			textblock.Inlines.Clear();
			textblock.Inlines.Add("Tools:\r\n\r\n");
			foreach (Tool t in recipe.Tools)
			{
				if (t.ToolTipID != 0)
				{
					textblock.Inlines.Add("- ");
					textblock.Inlines.Add(CreateInTextToolTip(t.Name, t.ToolTipID));
					textblock.Inlines.Add("\r\n");
				}
				else
				{
					textblock.Inlines.Add("- " + t.Name + "\r\n");
				}
			}
		}

		public void RunButtonsListUpdate()
		{
			int i = 0;
			foreach (UIElement e in recipeScrollableNavigationThingys)
			{
				if (i >= stepButtons.Count)
				{
					break;
				}
				stepButtons[i].StyleState = (VisibilityStyleState)IsUserElementVisible(e, Application.Current.MainWindow);
				i++;
			}
		}

		private RecipeUserVisible IsUserElementVisible(UIElement element, FrameworkElement container)
		{
			if (!element.IsVisible)
				return RecipeUserVisible.invisible;

			RecipeUserVisible visibility = RecipeUserVisible.invisible;
			if (container == null) throw new ArgumentNullException("container");

			Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.RenderSize.Width, element.RenderSize.Height));
			Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);

			if (rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight) || rect.IntersectsWith(bounds))
			{
				visibility = RecipeUserVisible.partial;
			}
			if (rect.Contains(bounds))
			{
				visibility = RecipeUserVisible.full;
			}
			return visibility;
		}

		public Run CreateInTextToolTip(string text, int ToolTipID)
		{

			Run popup_link = new Run();
			popup_link.Cursor = Cursors.Hand;
			popup_link.TextDecorations = TextDecorations.Underline;
			popup_link.Foreground = new SolidColorBrush(Colors.RoyalBlue);
			popup_link.MouseUp += new MouseButtonEventHandler(ToolTip_Click);
			popup_link.Text = text;
			popup_link.Tag = ToolTipID;
			return popup_link;
		}

		private void ToolTip_Click(object sender, RoutedEventArgs e)
		{
			Run link = (Run)sender;
			if ((int)link.Tag != 0)
			{
				ToolTip tip = recipe.GetToolTip((int)link.Tag);
				if (tip == null)
				{
					return;
				}
				PopupContent.TooltipImageStackPanel.Children.Clear();
				PopupContent.TooltipImageStackPanel.Visibility = Visibility.Collapsed;
				foreach (string imgname in tip.Images)
				{
					if (File.Exists("Images/" + imgname))
					{

                        RoundedCornerImage roundedImage = new RoundedCornerImage();
                        Image img = new Image();
                        img.Source = new BitmapImage(new Uri("Images/" + imgname, UriKind.Relative));
						//Thickness margin = roundedImage.RoundedCornerImageGrid.Margin;
						//margin.Right = 0;
						Thickness margin = new Thickness(5);
                        roundedImage.RoundedCornerImageGrid.Margin = margin;
                        roundedImage.RoundedCornerImageGrid.Height = 180;
                        roundedImage.RoundedCornerImageGrid.Width = (180.0 / img.Source.Height) * img.Source.Width;

                        ImageBrush image = new ImageBrush(img.Source) { Stretch = Stretch.UniformToFill, };
                        roundedImage.ImageBrushContent.Background = image;

                        ScaleTransform st = new ScaleTransform(1.05f, 1.05f, 0.5f, 0.5f);
                        roundedImage.ImageBrushContent.Background.RelativeTransform = st;

                        PopupContent.TooltipImageStackPanel.Children.Add(roundedImage);
                        PopupContent.TooltipImageStackPanel.Visibility = Visibility.Visible;
                    }
				}
				PopupContent.TooltipText.Text = tip.Text;
				InformationPopup.IsOpen = true;
			}
		}

		// Detect when the view has been scrolled so we close popups.
		private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			InformationPopup.IsOpen = false;
			RunButtonsListUpdate();
		}
        
    }
}
