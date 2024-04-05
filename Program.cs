using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TL;
using Vosk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Http;

namespace TGInstaAudioToText
{
    public class ConfigDesc : Attribute
    {
        public string Description = "";
    }

    static class Config
    {
        [ConfigDesc(Description = "Window Title")]
        public static string Title = "TGInstaAudioToText";

        [ConfigDesc(Description = "Model folder VOSK-API")]
        public static string ModelName = "";
        [ConfigDesc(Description = "Telegram Phone Number")]
        public static string TelegramPhone = "";
        [ConfigDesc(Description = "ApiId Telegram")]
        public static int TelegramApiId = 0;
        [ConfigDesc(Description = "ApiHash Telegram")]
        public static string TelegramApiHash = "";

        [ConfigDesc(Description = "Telegram User Name  (F2A?)")]
        public static string TelegramLoginName = "";
        [ConfigDesc(Description = "Telegram Password (F2A)")]
        public static string TelegramPassword = "";

        [ConfigDesc(Description = "Enable punctuation")]
        public static bool PunctuationEnabled = false;
        //[ConfigDesc(Description = "Python venv for punctuation")]
        //public static string PunctuationPuthonVenvPath = "";
        [ConfigDesc(Description = "Punctuation server")]
        public static string PunctuationServer = "http://127.0.0.1:8018";


        [ConfigDesc(Description = "Recognize inbound voice messages in personal chats")]
        public static bool InPersonal = true;
        [ConfigDesc(Description = "Recognize outbound voice messages in personal chats")]
        public static bool OutPersonal = true;
        [ConfigDesc(Description = "Recognize outbound voice messages in group chats")]
        public static bool OutGroup = true;

        [ConfigDesc(Description = "Text for: Bot trying to recognize text")]
        public static string TextBotTryingRecognize = "Бот пытается распознать текст";

        [ConfigDesc(Description = "Text for: Bot recognized text")]
        public static string TextBotRecognizedText = "Бот распознал текст";

        [ConfigDesc(Description = "Text for: Bot did't recognized text")]
        public static string TextBotDidntRecognizedText = "Бот не смог не распознать текст";
        //

        public static void Load(string FileName)
        {
            try
            {
                using TextReader reader = new StreamReader(new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));
                string line = reader.ReadLine();
                var members = typeof(Config).GetFields();
                var namedMembers = members.ToDictionary(p => p.Name.ToLower());
                while (line != null)
                {
                    if (!line.StartsWith("#"))
                    {
                        var keyval = line.Split('=', 2);
                        if (keyval.Length == 2)
                        {
                            var key = keyval[0].Trim().ToLower();
                            var value = keyval[1].Trim();

                            if (namedMembers.TryGetValue(key, out var FI))
                            {
                                var v = FI.GetValue(null);
                                Sys.FromString(value, ref v);
                                FI.SetValue(null, v);
                            }
                        }
                    }
                    line = reader.ReadLine()?.Trim();
                }

                StringBuilder SB = new StringBuilder();
                foreach (var member in members)
                {
                    ConfigDesc configDesc = (ConfigDesc)member.GetCustomAttribute(typeof(ConfigDesc), true);
                    if (configDesc != null)
                    {
                        SB.AppendLine($"#{configDesc.Description}");
                    }
                    SB.AppendLine($"{member.Name} = {member.GetValue(null)}");
                    SB.AppendLine();
                }


                FileStream stream = (FileStream)((StreamReader)reader).BaseStream;
                stream.SetLength(0);

                StreamWriter writer = new StreamWriter(stream);
                writer.Write(SB.ToString());
                writer.Flush();
            }
            catch { }
        }
    }
    class Program
    {
        static Model model;
        static WTelegram.Client client = null;
        static Object Locker = new Object();

        static Object PingLocker = new Object();
        static DateTime PongTime = default;

        public static DateTime AliveTimer
        {
            get
            {
                lock (PingLocker)
                {
                    return PongTime;
                }
            }
            set
            {
                lock (PingLocker)
                {
                    PongTime = value;
                }
            }
        }

        public static string SpeechToText(Model model, string FileName)
        {
            // Demo float array
            VoskRecognizer rec = new VoskRecognizer(model, 16000.0f);
            using (Stream source = File.OpenRead(FileName))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    float[] fbuffer = new float[bytesRead / 2];
                    for (int i = 0, n = 0; i < fbuffer.Length; i++, n += 2)
                    {
                        fbuffer[i] = BitConverter.ToInt16(buffer, n);
                    }
                    if (rec.AcceptWaveform(fbuffer, fbuffer.Length))
                    {
                        //Console.WriteLine(rec.Result());
                    }
                    else
                    {
                        //Console.WriteLine(rec.PartialResult());
                    }
                }
            }

            JObject data = (JObject)JsonConvert.DeserializeObject(rec.FinalResult());

            return data["text"].ToStringNoNull();
            //Console.WriteLine(rec.FinalResult());
        }

        static void CheckHealth()
        {
            new Thread((arg) =>
            {
                try
                {
                    Thread.Sleep(10000);
                    if (client.Disconnected)
                    {
                        Output.WriteLine($"{Output.NewLineIfNeed}CheckHealth: Disconnect detected", Output.TextError);
                        Reconnect().Wait();
                    }
                }
                catch { }
            }).Start();
        }

        static bool bInReconnect = false;
        static async Task<bool> Reconnect()
        {
            lock (Locker)
            {
                if (bInReconnect)
                {
                    return false;
                }
                bInReconnect = true;
            }
            client?.Dispose();
            client = null;

            try
            {
                while (!(await StartTelegram()))
                {
                    client?.Dispose();
                    client = null;
                    Output.WriteLine($"{Output.NewLineIfNeed}Reconnect: Can't StartTelegram retrying...", Output.TextError);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Output.WriteLine($"{Output.NewLineIfNeed}Reconnect: Connection still failing: {ex.Message}", Output.TextError);
            }
            lock (Locker)
            {
                bInReconnect = false;
            }
            return true;
        }
        private static async Task Client_OnOther(IObject arg)
        {
            if (arg is ReactorError err)
            {
                // typically: network connection was totally lost
                Output.WriteLine($"{Output.NewLineIfNeed}OnOther: Fatal reactor error: {err.Exception.Message}", Output.TextError);

                Output.WriteLine($"{Output.NewLineIfNeed}OnOther: Disposing the client and trying to reconnect in 5 seconds...", Output.TextDefault);
                await Reconnect();
            }
            else if (arg is User user)
            {

            }
            else if (arg is NewSessionCreated newSessionCreated)
            {

            }
            else if (arg is Pong pong)
            {
                AliveTimer = DateTime.Now;
                //Output.AutoNewLine();
                //Output.WriteLine($"OnOther: Pong: ping_id = {pong.ping_id}, ping_id = {pong.msg_id}", Output.TextComment);
            }
            else
            {
                Output.WriteLine($"{Output.NewLineIfNeed}Other: {arg.GetType().Name}", Output.TextComment);
            }
        }

        static async Task<string> PunctuateTextWebServer(string Text)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                UseProxy = false
            });

            var data = new
            {
                text = Text,
            };


            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(Config.PunctuationServer, content);

                if (!response.IsSuccessStatusCode)
                {
                    Output.WriteLine("Error: " + response.StatusCode, Output.TextError);
                    httpClient.Dispose();
                    return Text;
                }

                string result = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(result);
                Text = jsonResponse.text;

                //Console.WriteLine($"Server response: {jsonResponse}");
            }
            catch (Exception e)
            {
                Output.WriteLine($"Error: {e.Message}", Output.TextError);
            }
            finally
            {
                httpClient.Dispose();
            }
            return Text;
        }
        /*
        static string PunctuateText(string Text)
        {
            if (Config.PunctuationEnabled)
            {
                string VenvPath = Paths.ToAbsolute(Config.PunctuationPuthonVenvPath);
                StringBuilder SB = new StringBuilder();
                string ScriptPath;
                string Uid = $"_punktuation-{Guid.NewGuid()}";
                string PythonScriptPath = Path.Combine(Sys.GetAppPath(), Uid + ".py");
                string OutputPath = Path.Combine(Sys.GetAppPath(), Uid + ".txt");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ScriptPath = Path.Combine(Sys.GetAppPath(), Uid + ".cmd");
                    string ActivatePath = Path.Combine(VenvPath, "scripts", "activate.bat");
                    if (Config.PunctuationPuthonVenvPath.Length > 0)
                    {
                        SB.Append($"CALL \"{ActivatePath}\"\r\n");
                    }
                    SB.Append($"python \"{PythonScriptPath}\"\r\n");
                }
                else
                {
                    ScriptPath = Path.Combine(Sys.GetAppPath(), Uid + ".sh");
                    string ActivatePath = Path.Combine(VenvPath, "bin", "activate");
                    SB.Append($"#!/bin/bash -x\n");
                    if (Config.PunctuationPuthonVenvPath.Length > 0)
                    {
                        SB.Append($"source \"{ActivatePath}\"\n");
                    }
                    SB.Append($"python3 \"{PythonScriptPath}\"\n");
                }

                File.WriteAllText(ScriptPath, SB.ToString());
                File.WriteAllText(PythonScriptPath, $"from sbert_punc_case_ru import SbertPuncCase\r\nmodel = SbertPuncCase()\r\ntext = model.punctuate(\"{Text.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ")}\")\r\nwith open('{OutputPath.Replace("\\", "\\\\")}', 'w', encoding='utf-8') as f:\n    f.write(text)");

                ProcessStartInfo PSI;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    PSI = new ProcessStartInfo(ScriptPath);
                }
                else
                {
                    PSI = new ProcessStartInfo("bash", ScriptPath);
                }
                PSI.UseShellExecute = false;
                PSI.CreateNoWindow = true;
                var P = Process.Start(PSI);
                P.WaitForExit();
                if (File.Exists(OutputPath))
                {
                    Text = File.ReadAllText(OutputPath);
                    File.Delete(OutputPath);

                    File.Delete(ScriptPath);
                    File.Delete(PythonScriptPath);
                }
            }
            return Text;
        }
        */

        static void ConvertToWav(string SourceFileName, string TargetFileName)
        {
            //ProcessStartInfo PSI = new ProcessStartInfo(Path.Combine(Sys.GetAppPath(), "ffmpeg"), $"-i \"{SourceFileName}\" -acodec pcm_s16le -ac 1 -ar 16000 -y \"{TargetFileName}\"");
            ProcessStartInfo PSI = new ProcessStartInfo("ffmpeg", $"-i \"{SourceFileName}\" -acodec pcm_s16le -ac 1 -ar 16000 -y \"{TargetFileName}\"");
            PSI.UseShellExecute = false;
            PSI.RedirectStandardError = true;
            PSI.RedirectStandardOutput = true;

            var P = Process.Start(PSI);

            new Thread((args) => {
                try
                {
                    while (!P.StandardOutput.EndOfStream)
                    {
                        string V = P.StandardOutput.ReadLine();
                        if (V == null)
                        {
                            break;
                        }
                    }
                }
                catch { }
            }).Start();

            new Thread((args) =>
            {
                try
                {
                    while (!P.StandardError.EndOfStream)
                    {
                        string V = P.StandardOutput.ReadLine();
                        if (V == null)
                        {
                            break;
                        }
                    }
                }
                catch { }
            }).Start();

            P.WaitForExit();
        }

        async static Task<bool> StartTelegram()
        {
            string tmpPath = Path.Combine(Sys.GetAppPath(), "tmp");
            string dataPath = Path.Combine(Sys.GetAppPath(), "data");

            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            string ffmpegPath = Path.Combine(Sys.GetAppPath(), "ffmpeg");

            Output.Write("Starting telegram... ", Output.TextDefault);

            try
            {
                client = new WTelegram.Client(Config.TelegramApiId, Config.TelegramApiHash);
                client.MaxAutoReconnects = 1;
                client.OnOther += Client_OnOther;
                client.PingInterval = 60;

                client.OnUpdate += async (obj) =>
                {
                    AliveTimer = DateTime.Now;
                    //long SelfId = TL.InputPeer.Self.ID;
                    if (obj is TL.UpdateShortMessage shortMessage)
                    {
                        //Output.AutoNewLine();
                        //Output.WriteLine($"UpdateShortMessage: {shortMessage.message}", Output.TextInfo);
                    }
                    else if (obj is TL.Updates updates)
                    {
                        foreach (var update in updates.updates)
                        {
                            //Output.WriteLine($"Update {update.GetType().Name}", Output.TextInfo);
                            if (update is TL.UpdateNewMessage message)
                            {
                                if (message.message is TL.Message msg)
                                {
                                    bool Enabled = false;
                                    bool IsUserChat = (msg.Peer is TL.PeerUser);
                                    bool IsSelf = (msg.From == null ? (msg.Peer.ID == client.UserId) : (msg.From.ID == client.UserId));
                                    
                                    Enabled |= IsUserChat && IsSelf && Config.OutPersonal;
                                    Enabled |= IsUserChat && !IsSelf && Config.InPersonal;
                                    Enabled |= !IsUserChat && IsSelf && Config.OutGroup;

                                    if (Enabled)
                                    {
                                        //var User = updates.users[msg.Peer.ID];
                                        
                                        if ((msg.flags & Message.Flags.has_media) == Message.Flags.has_media)
                                        {
                                            if (msg.media is TL.MessageMediaDocument media)
                                            {
                                                if ((media.flags & MessageMediaDocument.Flags.voice) == MessageMediaDocument.Flags.voice)
                                                {
                                                    //Output.WriteLine($"{Output.NewLineIfNeed}Message {msg} IsUserChat={IsUserChat}, IsSelf={IsSelf}", Output.TextInfo);

                                                    if (media.document is TL.Document doc)
                                                    {
                                                        string Filename = doc.Filename;
                                                        if (Filename == null)
                                                        {
                                                            Filename = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.dd");
                                                        }
                                                        string tmpFileName = Path.Combine(tmpPath, $"{msg.Peer.ID}.{Filename}");

                                                        using (var fs = new FileStream(tmpFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                                        {
                                                            await client.DownloadFileAsync(doc, fs);
                                                        }

                                                        string WavFileName = tmpFileName + ".wav";

                                                        ConvertToWav(tmpFileName, WavFileName);


                                                        InputPeer inputPeer = null;
                                                        if (updates.users.TryGetValue(msg.Peer.ID, out var User))
                                                        {
                                                            inputPeer = User.ToInputPeer();
                                                        }
                                                        else if (updates.chats.TryGetValue(msg.Peer.ID, out var Chat))
                                                        {
                                                            inputPeer = Chat.ToInputPeer();
                                                        }


                                                        if (inputPeer != null)
                                                        {
                                                            var OutMsg = await client.SendMessageAsync(inputPeer, $"⏳ {Config.TextBotTryingRecognize}...");


                                                            try
                                                            {
                                                                Output.Write("Recognizing... ", Output.TextDefault);
                                                                string Text = SpeechToText(model, WavFileName);
                                                                Output.WriteLine("OK", Output.TextSuccess);
                                                                if (Config.PunctuationEnabled)
                                                                {
                                                                    Output.Write("Punctuating... ", Output.TextDefault);
                                                                    Text = await PunctuateTextWebServer(Text);
                                                                    Output.WriteLine("OK", Output.TextSuccess);
                                                                }
                                                                
                                                                Output.WriteLine($"Text: {Text}", Output.TextInfo);
                                                                await client.Messages_EditMessage(inputPeer, OutMsg.ID, $"🤖 {Config.TextBotRecognizedText}:\r\n\r\n{Text}");
                                                                await client.Messages_MarkDialogUnread(inputPeer);


                                                                File.Delete(tmpFileName);
                                                                File.Delete(WavFileName);
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Output.WriteLine($"Error: {ex.Message}", Output.TextError);
                                                                try
                                                                {
                                                                    await client.Messages_EditMessage(inputPeer, OutMsg.ID, $"🙁 {Config.TextBotDidntRecognizedText}");
                                                                }
                                                                catch { }
                                                            }
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                    else if (obj is TL.UpdateShort)
                    {

                    }
                    else
                    {
                        //Output.WriteLine($"OnUpdate {obj.GetType().Name}", Output.TextInfo);
                    }

                    //if (obj)
                    int KKK = 0;
                };
                await DoLogin(Config.TelegramPhone);

                if (client.User == null)
                {
                    throw new Exception("loginUser == null");
                }
                //Output.WriteLine($"client.User.ID = {client.User.ID}, client.User.MainUsername = {client.User.MainUsername}");
                Output.WriteLine($"OK", Output.TextSuccess);
                return true;
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Error: {ex.Message}", Output.TextError);
                return false;
            }
        }

        static async Task DoLogin(string loginInfo)
        {
            while (client.User == null)
                switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                {
                    case "verification_code":
                        Output.Write($"{Output.NewLineIfNeed}Code requered: ", Output.TextWarning);
                        loginInfo = Console.ReadLine();
                        Output.WriteLine($"{Output.NewLineIfNeed}Code: {loginInfo} ({loginInfo.Length})", Output.TextDefault);
                        Output.Write($"{Output.NewLineIfNeed}Telegram login... ", Output.TextDefault);
                        break;
                    case "name":
                        if (Config.TelegramLoginName.Length == 0)
                        {
                            Output.Write($"{Output.NewLineIfNeed}Name requered: ", Output.TextWarning);
                            loginInfo = Console.ReadLine();
                            Output.Write($"{Output.NewLineIfNeed}Telegram login... ", Output.TextDefault);
                        }
                        else
                        {
                            loginInfo = Config.TelegramLoginName;
                        }
                        break;    // if sign-up is required (first/last_name)
                    case "password":
                        if (Config.TelegramPassword.Length == 0)
                        {
                            Output.Write($"{Output.NewLineIfNeed}Password requered: ", Output.TextWarning);
                            loginInfo = Console.ReadLine();
                            Output.Write($"{Output.NewLineIfNeed}Telegtam login... ", Output.TextDefault);
                        }
                        else
                        {
                            loginInfo = Config.TelegramPassword;
                        }
                        break; // if user has enabled 2FA
                    default:
                        loginInfo = null;
                        break;
                }
            //Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
        }

        static void CheckFFmpeg()
        {
            ProcessStartInfo PSI = new ProcessStartInfo("ffmpeg", $"-version");
            PSI.UseShellExecute = false;
            PSI.RedirectStandardError = true;
            PSI.RedirectStandardOutput = true;

            var P = Process.Start(PSI);

            new Thread((args) =>
            {
                try
                {
                    while (!P.StandardOutput.EndOfStream)
                    {
                        string V = P.StandardOutput.ReadLine();
                    }
                }
                catch { }
            }).Start();

            new Thread((args) =>
            {
                try
                {
                    while (!P.StandardError.EndOfStream)
                    {
                        string V = P.StandardOutput.ReadLine();
                    }
                }
                catch { }
            }).Start();

            P.WaitForExit();
        }

        static string ScriptExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "cmd";
            }
            return "sh";
        }

        static void Main(string[] args)
        {
            Config.Load(Sys.GetConfigFileName());

            Console.Title = Config.Title;

            WTelegram.Helpers.Log = (lvl, str) => { };
            Vosk.Vosk.SetLogLevel(-1);

            /*
            if (Config.PunctuationEnabled)
            {
                try
                {
                    Output.Write("Checking Punctuation... ", Output.TextDefault);
                    var SourceText = "Проверка пунктуации а потом еще одна но не долгая а так все хорошо возможно";
                    var Text = PunctuateText(SourceText);
                    if (Text == SourceText)
                    {
                        throw new Exception($"Failed run scripts");
                    }
                    Output.WriteLine("OK", Output.TextSuccess);
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Error: {ex.Message}. Run files '_punktuation-*.{ScriptExtension()}' in application's folder manual and find what's wrong or tutn off PunctuationEnabled option in cfg file", Output.TextError);
                    //
                    return;
                }
            }
            */

            try
            {
                Output.Write("Checking FFMpeg... ", Output.TextDefault);
                CheckFFmpeg();
                Output.WriteLine("OK", Output.TextSuccess);
            }
            catch (Exception ex)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Output.WriteLine($"Not Found: Download lastest version from https://ffmpeg.org/download.html#build-windows and put ffmpeg.exe to application's folder", Output.TextError);
                }
                else
                {
                    Output.WriteLine($"Not Found: Install ffmpeg, may be \"sudo apt-get install ffmpeg\"", Output.TextError);
                }
                //Output.WriteLine($"Error: {ex.Message}", Output.TextError);
                return;
            }
            
            try
            {
                Output.Write("Loading model... ", Output.TextDefault);
                if (!Directory.Exists(Config.ModelName))
                {
                    Output.WriteLine($"Model '{Config.ModelName}' Not Found: Download model from https://alphacephei.com/vosk/models and put model's folder to application's folder", Output.TextError);
                    return;
                }
                else
                {
                    model = new Model(Config.ModelName);
                    Output.WriteLine("OK", Output.TextSuccess);
                }
                
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Error: {ex.Message}", Output.TextError);
                return;
            }

            new Thread(async (arg) =>
            {
                while (!(await StartTelegram()))
                {
                    Console.WriteLine("Can't StartTelegram retrying...");
                    Thread.Sleep(1000);
                }
            }).Start();
            CheckHealth();
            //*/
            while (true)
            {
                Thread.Sleep(1000);
                //Console.ReadKey();
            }

            
        }
    }
}
