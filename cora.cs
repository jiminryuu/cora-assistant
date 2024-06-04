using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Timers;
using System.Net.Http;
using System.Net.Http.Json;

namespace VoiceAssistant
{
    public partial class cora : Form
    {
        private SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
        private SpeechRecognitionEngine speechRecog = new SpeechRecognitionEngine();
        private SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();
        private HttpClient httpClient = new HttpClient();
        private string weatherApiKey = "removed this personal API Key for weather retrieval";
        private static System.Timers.Timer alarmTimer;
        private static DateTime alarmTime;


        private Random rnd = new Random();
        private DateTime currTime = DateTime.Now;
        private bool recognized_speech = true;

        public cora()
        {
            InitializeComponent();
        }

        private void cora_load(object sender, EventArgs e)
        {
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammarAsync(new Grammar(new GrammarBuilder(new Choices(File.ReadAllLines(@"TextFiles\DefaultCommands.txt")))));
            recognizer.RequestRecognizerUpdate();
            recognizer.LoadGrammar(new DictationGrammar());
            recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(speechRecognizerCora);
            recognizer.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(_recognizer_SpeechRecognized);
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            speechSynthesizer.SpeakStarted += VoiceAssistant_SpeakStarted;
            speechSynthesizer.SpeakCompleted += VoiceAssistant_SpeakCompleted;

            speechRecog.SetInputToDefaultAudioDevice();
            speechRecog.LoadGrammarAsync(new Grammar(new GrammarBuilder(new Choices(File.ReadAllLines(@"TextFiles\DefaultCommands.txt")))));
            recognizer.RequestRecognizerUpdate();
            speechRecog.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(startListening_SpeechRecognized);
        }

        private void VoiceAssistant_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            recognized_speech = true;
        }

        private void VoiceAssistant_SpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            recognized_speech = false;
        }

        public async Task GetWeatherAsync(string city)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<WeatherResponse>($"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={weatherApiKey}&units=imperial");
                if (response != null)
                {
                    var weatherDescription = response.Weather[0].Description;
                    var temperature = response.Main.Temp;
                    speechSynthesizer.SpeakAsync($"The current weather in {city} is {weatherDescription} with a temperature of {temperature} degrees Fahrenheit.");
                }
            }
            catch (Exception ex)
            {
                speechSynthesizer.SpeakAsync("Sorry, I couldn't get the weather information.");
                Console.WriteLine(ex.Message);
            }
        }


        private void speechRecognizerCora(object? sender, SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;

            if (recognized_speech == true)
            {
                switch (speech)
                {
                    case "Hi assistant":
                        speechSynthesizer.SpeakAsync("hello");
                        break;

                    case "What time is it?":
                        speechSynthesizer.SpeakAsync(DateTime.Now.ToString("h mm tt"));
                        break;

                    case "Stop talking":
                        speechSynthesizer.SpeakAsync("Okay, I will stop talking");
                        break;

                    case "Play my favorite song":
                        speechSynthesizer.SpeakAsync("Okay, playing Hotel California");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "chrome.exe",
                            Arguments = "https://www.youtube.com/watch?v=IkWNGOjHHzs",
                            UseShellExecute = true
                        });
                        break;

                    case "What's the weather like?":
                        speechSynthesizer.SpeakAsync("i will found out the temperature in winnipeg");
                        string city = "Winnipeg";
                        GetWeatherAsync(city).Wait();
                        break;

                    case "Can you set an alarm for me?":
                        speechSynthesizer.SpeakAsync("Sure, what time would you like to set the alarm for?");
                        recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognize_setAlarm);
                        recognizer.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(_recognizer_SpeechRecognized);
                        break;

                    case "Tell me a joke":
                        speechSynthesizer.SpeakAsync("Why don't scientists trust atoms? Because they make up everything!");
                        break;

                    case "What's the date today?":
                        speechSynthesizer.SpeakAsync($"Today's date is {DateTime.Now.ToString("MMMM dd, yyyy")}");
                        break;

                    case "Open notepad":
                        speechSynthesizer.SpeakAsync("Opening Notepad");
                        Process.Start("notepad.exe");
                        break;

                    default:
                        speechSynthesizer.SpeakAsync("Sorry, I didn't understand that command.");
                        break;
                }
            }
        }
        public class WeatherResponse
        {
            public Weather[] Weather { get; set; }
            public Main Main { get; set; }
        }

        public class Weather
        {
            public string Description { get; set; }
        }
        public class Main
        {
            public float Temp { get; set; }
        }

        public void SetAlarm(DateTime time)
        {
            alarmTime = time;
            speechSynthesizer.SpeakAsync($"Alarm set for {alarmTime.ToString("h:mm tt")}");

            double interval = (alarmTime - DateTime.Now).TotalMilliseconds;
            if (interval <= 0)
            {
                speechSynthesizer.SpeakAsync("The time you entered is in the past. Please enter a future time.");
                return;
            }

            if (alarmTimer != null)
            {
                alarmTimer.Stop();
                alarmTimer.Dispose();
            }

            alarmTimer = new System.Timers.Timer(interval);
            alarmTimer.Elapsed += OnAlarmTriggered;
            alarmTimer.AutoReset = false;
            alarmTimer.Start();
        }

        private  void OnAlarmTriggered(object sender, ElapsedEventArgs e)
        {
            speechSynthesizer.SpeakAsync("Alarm ringing! Time to wake up!");
            alarmTimer.Stop();
            alarmTimer.Dispose();
        }

        private void recognize_setAlarm(object? sender, SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text;
            if (speech.ToLower().EndsWith('m'))
            {

                try
                {
                 SetAlarm(DateTime.Parse(speech)); 
                } catch (Exception ex)
                {
                   speechSynthesizer.SpeakAsync("Please tell me a time only");
                    
                }

                
            }

        }
}