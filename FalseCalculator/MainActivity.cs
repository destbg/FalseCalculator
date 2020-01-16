using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FalseCalculator.Enums;
using FalseCalculator.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FalseCalculator
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : AppCompatActivity
    {
        private TextView calculatorText;
        private HorizontalScrollView scrollView;
        private GridLayout layout;

        private readonly IList<string> numbers = new List<string>();
        private readonly IList<char> operators = new List<char>();
        private string secretCode = string.Empty;
        private bool addNumber = true;
        private bool hasDot;
        private bool hasResult;

        private SettingsModel settings = new SettingsModel();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            InitMainView();

            if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                RequestPermissions(new string[] {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage
                }, 0);
            }
            else
            {
                ReadFile();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        [Java.Interop.Export("ButtonClick")]
        public void ButtonClick(View v)
        {
            var button = (Button)v;
            secretCode += button.Text;
            if (secretCode == "277353")
            {
                Erase();
                SetContentView(Resource.Layout.secret);
                secretCode = string.Empty;
                return;
            }
            if ("1234567890.".Contains(button.Text))
            {
                AddDigitOrDecimalPoint(button.Text);
            }
            else if ("÷×+-".Contains(button.Text))
            {
                AddOperator(button.Text[0]);
            }
            else if ("=" == button.Text)
            {
                Calculate();
            }
            else
            {
                Erase();
            }
        }

        [Java.Interop.Export("ChooseCalculator")]
        public void ChooseCalculator(View v)
        {
            var button = (Button)v;
            settings.CalculationType = button.Text switch
            {
                "Normal calculator" => CalculationType.Normal,
                "Add 1 to result" => CalculationType.AddOne,
                _ => CalculationType.OnlyWord,
            };

            if (button.Text == "Only word")
            {
                var layoutInflater = LayoutInflater.From(this);
                var view = layoutInflater.Inflate(Resource.Layout.enter_word, null);
                var alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                alertbuilder.SetView(view);
                var userdata = view.FindViewById<EditText>(Resource.Id.textinput_counter);
                alertbuilder.SetCancelable(false)
                .SetPositiveButton("Submit", delegate
                {
                    settings.ResponceWord = userdata.Text;
                    WriteFile();
                    SetContentView(Resource.Layout.activity_main);
                    InitMainView();
                })
                .SetNegativeButton("Cancel", delegate
                {
                    alertbuilder.Dispose();
                    WriteFile();
                    SetContentView(Resource.Layout.activity_main);
                    InitMainView();
                });
                var dialog = alertbuilder.Create();
                dialog.Show();
            }
            else
            {
                WriteFile();
                SetContentView(Resource.Layout.activity_main);
                InitMainView();
            }
        }

        private void AddDigitOrDecimalPoint(string value)
        {
            if (hasResult)
            {
                numbers.Clear();
                hasResult = false;
            }

            if (value == ".")
            {
                if (numbers.Count > 0 && numbers[^1].Contains("."))
                    return;
                hasDot = true;
            }
            else
            {
                hasDot = false;
            }

            if (addNumber && numbers.Count != 0)
            {
                numbers[^1] += value;
            }
            else
            {
                numbers.Add(value);
            }
            addNumber = true;

            UpdateCalculatorText();
        }

        private void AddOperator(char value)
        {
            if (numbers.Count == operators.Count)
                return;

            if (hasDot)
            {
                numbers[^1] = numbers[^1].Replace(".", "");
                hasDot = false;
            }

            if (!addNumber && operators.Count != 0)
            {
                operators[^1] = value;
            }
            else
            {
                operators.Add(value);
            }

            addNumber = false;
            hasResult = false;

            UpdateCalculatorText();
        }

        private void Calculate()
        {
            if (numbers.Count == 0)
            {
                Erase();
                calculatorText.Text = "0";
                return;
            }

            var result = settings.CalculationType switch
            {
                CalculationType.Normal => CalculateNormal().ToString(),
                CalculationType.AddOne => (CalculateNormal() + 1).ToString(),
                _ => settings.ResponceWord
            };

            numbers.Clear();
            operators.Clear();
            addNumber = true;
            hasDot = false;
            hasResult = true;
            secretCode = string.Empty;
            calculatorText.Text = result;
            numbers.Add(result);
        }

        private void Erase()
        {
            numbers.Clear();
            operators.Clear();
            addNumber = true;
            hasDot = false;
            hasResult = false;
            secretCode = string.Empty;
            calculatorText.Text = "";
        }

        private void UpdateCalculatorText()
        {
            var text = new StringBuilder();
            for (int i = 0; i < numbers.Count; i++)
            {
                text.Append(numbers[i]);
                if (operators.Count != i)
                    text.Append(' ').Append(operators[i]).Append(' ');
            }
            calculatorText.Text = text.ToString();
        }

        private (int, char) FindFirstOperator()
        {
            for (int i = 0; i < operators.Count; i++)
            {
                if ("÷×".Contains(operators[i]))
                    return (i, operators[i]);
            }

            return (0, operators[0]);
        }

        private void InitMainView()
        {
            calculatorText = FindViewById<TextView>(Resource.Id.calculator_text_view);
            scrollView = FindViewById<HorizontalScrollView>(Resource.Id.horizontalScrollView1);
            scrollView = FindViewById<HorizontalScrollView>(Resource.Id.horizontalScrollView1);
            layout = FindViewById<GridLayout>(Resource.Id.gridLayout1);
            calculatorText.Text = "";
        }

        private double CalculateNormal()
        {
            var doubleNumbers = numbers.Select(f => double.Parse(f)).ToList();

            if (operators.Count != 0)
            {
                if (doubleNumbers.Count == operators.Count)
                    operators.RemoveAt(operators.Count - 1);

                while (doubleNumbers.Count != 1)
                {
                    var (index, @operator) = FindFirstOperator();
                    switch (@operator)
                    {
                        case '+':
                            doubleNumbers[index] += doubleNumbers[index + 1];
                            break;
                        case '-':
                            doubleNumbers[index] -= doubleNumbers[index + 1];
                            break;
                        case '÷':
                            doubleNumbers[index] /= doubleNumbers[index + 1];
                            break;
                        default:
                            doubleNumbers[index] *= doubleNumbers[index + 1];
                            break;
                    }
                    doubleNumbers.RemoveAt(index + 1);
                    operators.RemoveAt(index);
                }
            }

            return doubleNumbers[0];
        }

        private void ReadFile()
        {
            var filename = Path.Combine(Environment.ExternalStorageDirectory.Path, "settings.json");

            if (File.Exists(filename))
                settings = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText(filename));
        }

        private void WriteFile()
        {
            var filename = Path.Combine(Environment.ExternalStorageDirectory.Path, "settings.json");
            File.WriteAllText(filename, JsonConvert.SerializeObject(settings));
        }
    }
}