﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using System.Threading.Channels;

namespace WebJukebox.Pages
{
    public class IndexModel : PageModel
    {
        // Message receives the dynamic html code
        public string Message { get; private set; } = "PageModel in C#";
        
        // Shortcuts for known performers
        private static readonly string d3m = "Dominique Domet de Mont";
        private static readonly string lml = "Louis-Marie Lardic";
        
        // The static playlist
        private static Title[] playList = {
            // The first item is reserved for live recording: file and timing will be updated when record is complete.
            // Since the file name is using the recording date & time, subsequent recordings do not override previous ones, even if the playlist shows only the latest
            new Title("EMPTY.MID", "[Cliquez sur Enregistrer pour ajouter une pièce ici...]", "1,5,2", null, null),
            
            // Other items are known and documented
            new Title("LISZT.MID", "Franz Liszt : Prélude et Fugue sur B.A.C.H. (10')", "15, 617, 20", "https://fr.wikipedia.org/wiki/Fantasie_und_Fuge_%C3%BCber_das_Thema_B-A-C-H#p-lang-btn", lml),
            new Title("MESSIAEN.MID", "Olivier Messiaen : Banquet céleste (6')", "5, 375, 19", "https://en.wikipedia.org/wiki/Le_Banquet_C%C3%A9leste#p-lang-btn", d3m),
            new Title("ASCENSIO.MID", "Olivier Messiaen : Prière du Christ montant vers son Père (7')", "8, 425, 10", "https://fr.wikipedia.org/wiki/Olivier_Messiaen#p-lang-btn", d3m),
            new Title("WAGNER.MID", "Richard Wagner : Mort de Isolde, transcription (8'40)", "8, 510, 2", "https://fr.wikipedia.org/wiki/Liebestod#p-lang-btn", lml),
            new Title("DUPRE.MID", "Marcel Dupré : Prélude et Fugue en sol mineur (7')", "12, 403, 10", "https://fr.wikipedia.org/wiki/Marcel_Dupr%C3%A9#p-lang-btn", lml),
            new Title("COUPERIN.MID", "François Couperin : Tierce en taille (4')", "6, 240, 2", "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", d3m),
            new Title("FRANCK.MID", "César Franck : Troisième Choral (10'20)", "13, 622, 20", "https://fr.wikipedia.org/wiki/C%C3%A9sar_Franck#p-lang-btn", d3m),
            new Title("CHROMORN.MID", "François Couperin : Chromorne en taille (4')", "5, 233, 2", "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", d3m),
            new Title("TOCAREM.MID", "J.S. Bach: Toccata en Ré mineur (2'30)", "2, 145, 5", "https://fr.wikipedia.org/wiki/Toccata_et_fugue_en_r%C3%A9_mineur#p-lang-btn", d3m),
            new Title("LANGLAIS.MID", "Jean Langlais : Chant de Paix (2'30)", "4, 150, 4", "https://fr.wikipedia.org/wiki/Jean_Langlais#p-lang-btn", d3m),
            new Title("GUILMANT.MID", "Alexandre Guilmant : Noël 'Or dites-nous Marie' (2'20)", "6, 130, 4", "https://fr.wikipedia.org/wiki/Alexandre_Guilmant#p-lang-btn", d3m),
            new Title("DISPUTE.MID", "Yann Tiersen : La Dispute - imitation musette (2'20)", "4, 133, 2", "https://fr.wikipedia.org/wiki/Le_Fabuleux_Destin_d%27Am%C3%A9lie_Poulain_(bande_originale)#p-lang-btn", d3m),
            new Title("HIMMEL.MID", "JS Bach : Nun schleuss den Himmel auf (2'10)", "5, 125, 2", "https://fr.wikipedia.org/wiki/Orgelb%C3%BCchlein#p-lang-btn", d3m)
        };
        
        // MIDI files location on the server, also used to store new recordings
        private static string CatalogPath="C:/Users/domin/Desktop/Jukebox/";

        // List of ids accepted on GET requests: 
        // - 0..N-1: the title to play
        // - N... commands stop, pause, resume, record
        private static int stopId = playList.Length;
        private static int pauseId = stopId+1;
        private static int resumeId = pauseId+1;
        private static int recordId = resumeId + 1;

        private static Title? currentTitle =null;

        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Build the welcome page: 
        // Display the last error if any, the list of titles available for playing, the MIDI devices in/out
        // Two actions: Refresh and Record
        private void Welcome()
        {
            Message = "";
            string err = Title.getLastError();
            if (err != null) Message += "<p>Error: " + err + "</p>";
            currentTitle = null;

            if (OutputDevice.GetDevicesCount() > 0)
            {
                OutputDevice lastMidiDevice = OutputDevice.GetByIndex(OutputDevice.GetDevicesCount() - 1);
                Title.SetMidiDevice(lastMidiDevice);
                InputDevice lastMidiRecordingDevice=null;
                if (InputDevice.GetDevicesCount() > 0)
                {
                    lastMidiRecordingDevice = InputDevice.GetByIndex(InputDevice.GetDevicesCount() - 1);
                    Title.SetMidiRecordingDevice(lastMidiRecordingDevice);
                }
                Title.SetCatalogPath(CatalogPath);
                Message += "<ul><li>Périphérique de sortie MIDI : " + lastMidiDevice.Name+ "</li>";
                if(lastMidiRecordingDevice != null) Message += "<li>Périphérique d'enregistrement MIDI : " + lastMidiRecordingDevice.Name + "</li>";
                Message += "<li>Répertoire du catalogue : " + CatalogPath + "</li></ul>";
                Message += "<table><tr><td align=left>";
                if (lastMidiRecordingDevice != null)
                {
                    if(Title.IsFree())Message += "<a href=" + recordId + "> Enregistrer</a>";
                }
                Message += "</td><td align=right><a href='/'> Actualiser</a></tr></table>";

                Message += "<h3>Liste des pièces disponibles</h3><ul>";
                for (int i = 0; i < playList.Length; i++)
                {
                    Message += "<li><a href=" + i + ">" + playList[i].description + "</a></li>";
                };
                Message += "</ul>";
            }
            else
            {
                Message = "<h3>Aucun périphérique de sortie MIDI détecté/h3>";
            }
            
        }
        
        // Build the Record page
        // Display the recording status
        // Two actions: Refresh and Stop
        private void Record()
        {
            string recordingState = Title.getRecordingInfo();
            Message = "<h3>Enregistrement en cours</h3><p>" + recordingState + "</p>";
            Message += "<table><tr>";
            Message += "<td align=left><a href=" + stopId + ">Arrêter</a></td>";
            Message += "<td align=right><a href=" + recordId + "> Actualiser</a></td>";
            Message += "</tr></table>";
        }
        
        // Build the Current page
        // Display the current play, with an optional count down (typically while playing, not paused)
        // Three actions: Stop/Pause-Resume/Refresh
        private void Current(bool showCountDown)
        {
            if(currentTitle == null) { Welcome(); return; }

            Message = "<h3>Pièce en cours d'audition</h3><p>" + currentTitle.description + "</p>";
            string err = Title.getLastError();
            if (err != null) Message += "<p>Error: " + err + "</p>";
            if (currentTitle.performer != null)
            {
                Message += "<h5>Interprétation : "+currentTitle.performer+"</h5>";
            }
            Message += "<table><tr>";
            Message += "<td align=left><a href=" + stopId + ">Arrêter</a></td>";
            if (Title.IsPlaying())
            {
                Message += "<td align=center><a href=" + pauseId + ">Pause</a></td>";
            }
            else
            {
                Message += "<td align=center><a href=" + resumeId + ">Reprendre</a></td>";
            }

            Message += "<td align=right><a id='refresh' href='/'> Actualiser</a></td>";
            Message += "</tr></table>";
            if (showCountDown) CountDown();
            if (currentTitle.doc != null)
            {
                Message += @"<iframe width=100% height=400px src=""" + currentTitle.doc+@""" title=""Wikipedia""></iframe> ";
            }
        }
        
        // The cound down display 
        // This is a javascript fragment receiving dynamically a 'distances' variable providing the play timing as a table of 4 durations expressed in seconds:
        // - load: delay before the first note is played
        // - perform: the actual play duration
        // - unload: delay after the last note is played
        // - current: the current position in the play
        // The javascript countDown function will refresh the HTML variable with id countDown at 'rate' period with the current phase and the remaining time in this phase
        private void CountDown() 
        { 
            Message += @"<p id=""countDown""></p>
<script>
var distances=[" + currentTitle.timing + ","+Title.GetElapsed()+ @"]
var phases=[""Chargement : "", ""Exécution : "", ""Déchargement : ""]
// Update the count down every 1 second
var rate=1000;
var phase=0;
var elapsed = distances[phases.length]
phases.forEach(function(p, i){
  if(elapsed <= distances[i]){
    distances[i]-=elapsed
    elapsed= distances[phases.length]=0
    }else{
  phase=i
    elapsed-=distances[i]
    distances[i]=0
    }
})
var distance = distances[phase]
var refreshLink = document.getElementById(""refresh"")
function doRefresh(){
  refreshLink.click();
}
function countDown() {
  var minutes = Math.floor((distance % (60 * 60)) / 60);
  var seconds = Math.floor(distance % 60);
  document.getElementById(""countDown"").innerHTML = phases[phase] + (minutes?minutes+""mn "":"""")+seconds+""s""
  if(distance == 0 && phase < phases.length-1){
   phase++
   distance = distances[phase]
  }
  if(distance > 0)setTimeout(countDown, rate);
  distance--
}

if(rate)setTimeout(countDown, rate);
setTimeout(doRefresh, 1000*(distances[0]+distances[1]+distances[2]-distances[3]+4))
</script>
";
        }

        // The GET request processing
        // List of ids accepted: 
        // - 0..N-1: the title to play
        // - N... commands stop, pause, resume, record
        // Store the new recording in the first element of the playlist (while not erasing previous recorded files)
        public void OnGet(int? id)
        {
            if (id != null)
            {
                if (Title.IsFree() && id < playList.Length)
                {
                    _logger.LogInformation("Starting playing id: {int}", id);
                    currentTitle = playList[(int)id];
                    currentTitle.Start();
                    Current(true);            
                } else if(id == stopId) {
                    _logger.LogInformation("Stopping id: {int}", id);
                    if(currentTitle != null)currentTitle.Cancel();
                    Welcome();
                }
                else if (id == pauseId)
                {
                    _logger.LogInformation("Pausing id: {int}", id);
                    if (currentTitle != null)currentTitle.Pause();
                    Current(false);
                }
                else if (id == resumeId)
                {
                    _logger.LogInformation("Resuming id: {int}", id);
                    if (currentTitle != null)currentTitle.Resume();
                    Current(true);
                }
                else if(id == recordId)
                {
                    if (Title.IsFree())
                    {
                        _logger.LogInformation("Starting new recording");
                        // Store the new recording in the first element of the playlist
                        playList[0] = currentTitle = Title.Record();
                        Record();
                    }
                    else if (Title.IsRecording())
                    {
                        _logger.LogInformation("Displaying current recording");
                        Record();
                    }
                    else if (Title.IsPlaying())
                    {
                        _logger.LogInformation("Displaying current play");
                        Current(Title.IsPlaying());
                    }
                    else Welcome();
                }
            }
            else
            {
                if (Title.IsFree())
                {
                    _logger.LogInformation("Displaying Welcome");
                    Welcome();
                }else if (Title.IsRecording())
                {
                    _logger.LogInformation("Displaying current record");
                    Record();
                }
                else
                {
                    _logger.LogInformation("Displaying current play");
                    Current(Title.IsPlaying());
                }
            }
        }
    }

    // The implementation of a title to play / record
    class Title
    {
        private static Playback _playback;
        private static OutputDevice outputDevice;
        private static InputDevice inputDevice;
        enum State
        {
            idle,
            playing,
            recording
        } 
        private static State state=State.idle;
        private static string playlistPath;
        private static string lastError;
        private static Timer heartBeat;
        private static Recording recording;
        private static MetricTimeSpan firstRecordedNoteTime, lastRecordedNoteTime;

        public Title(string aFile, string aDescription, string aTiming, string? aDoc, string? aPerformer)
        {
            file = aFile;
            description = aDescription;
            timing = aTiming;
            doc = aDoc;
            performer = aPerformer;
        }
        public static string getLastError() { string err = lastError; lastError = null;  return err; }   
        public static bool IsPlaying() { return _playback != null && _playback.IsRunning; }
        public static bool IsFree() { return state == State.idle; }
        public static bool IsRecording() { return state == State.recording; }
        public static void SetMidiDevice(OutputDevice d)
        {
            if (outputDevice != null) outputDevice.Dispose();
            outputDevice = d;
        }
        public static void SetMidiRecordingDevice(InputDevice d)
        {
            if (inputDevice != null) inputDevice.Dispose();
           inputDevice = d;
           inputDevice.EventReceived += OnEventReceived;
        }
        public static void SetCatalogPath(string p) { playlistPath = p; }
        public static double GetElapsed() {
            if (_playback == null) return 0;
            MetricTimeSpan result = (MetricTimeSpan)_playback.GetCurrentTime(TimeSpanType.Metric);
            return result.TotalSeconds;
        }
        public static void Heartbeat(object? state)
        {
            try
            {
                // Send a void command to keep the device alive: open a fake swell box
                ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
                swellOn.Channel = (FourBitNumber)0; outputDevice.SendEvent(swellOn);
            }catch(Exception e) { Console.Out.WriteLine("Cannot send heartbeat: "+e.Message); };
        }
        public static void SwellOn()
        {
            // Open the swell boxes
            ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
            swellOn.Channel = (FourBitNumber)3; outputDevice.SendEvent(swellOn);
            swellOn.Channel = (FourBitNumber)2; outputDevice.SendEvent(swellOn);
        }
        
        // Start a new recording on the input device
        // Name the file using the current date and time to make it unique
        public static Title Record()
        {
            firstRecordedNoteTime = null;
            lastRecordedNoteTime = null;
            recording = new Recording(TempoMap.Default, inputDevice);
            inputDevice.StartEventsListening();
            recording.Start();
            state = State.recording;
            return new Title(
                "Recorded" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".mid", 
                "Enregistrement " + DateTime.Now.ToString(), 
                "1,2,1", null, null);
        }
        public  static String getRecordingInfo()
        {
            if(recording == null) return null;
            String firstNote = "Aucune note reçue";
            String lastNote = "Aucune note terminée";
            if(firstRecordedNoteTime != null)
            {
                firstNote = "1ère note : " + firstRecordedNoteTime.TotalSeconds;
            }
            if (lastRecordedNoteTime != null)
            {
                lastNote = "Dernière note : " + lastRecordedNoteTime.TotalSeconds;
            }
            return "<p>IsRunning: "+recording.IsRunning+"</p><p>IsListening: "+inputDevice.IsListeningForEvents+
                "</p><p>Durée: " +recording.GetDuration(TimeSpanType.Metric).ToString()+
                "</p><p>" + firstNote+"</p>"+
                "</p><p>" + lastNote + "</p>";
        }
        
        // Record specific events timing: first note, last note
        private static void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            if (e.Event.EventType == MidiEventType.NoteOn)
            {
                if(firstRecordedNoteTime == null)firstRecordedNoteTime = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
                lastRecordedNoteTime = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
            }
        }
        
        // Start a playback
        public void Start() {
            if (heartBeat != null) heartBeat.Dispose();
            SwellOn();
            var midiFile = MidiFile.Read(playlistPath+file);
            _playback = midiFile.GetPlayback(outputDevice);
            _playback.DeviceErrorOccurred += OnError;
            _playback.Start();
            _playback.Finished += OnFinished;
            state = State.playing;
    }
        private static void OnError(object sender, EventArgs e)
        {
            lastError = e.ToString();
        }
        private static void OnFinished(object sender, EventArgs e)
        {
            heartBeat = new Timer(Heartbeat, null, 10000, 1000); 
            _playback.Dispose();
            state = State.idle;
        }

        // Stop the current action
        // In recording case, save the file and update the timing
        public void Cancel() {
            switch (state)
            {
                case State.playing:
                    heartBeat = new Timer(Heartbeat, null, 10000, 1000);
                    _playback.Stop();
                    _playback.Dispose();
                    break;
                case State.recording:
                    MetricTimeSpan end = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
                    if(firstRecordedNoteTime!=null && lastRecordedNoteTime!=null)timing = 
                        (int)firstRecordedNoteTime.TotalSeconds + 
                        "," + (int)(lastRecordedNoteTime.TotalSeconds - firstRecordedNoteTime.TotalSeconds ) +
                        "," + (int)(end.TotalSeconds - lastRecordedNoteTime.TotalSeconds);
                    description += " ("+end.Minutes+"'"+end.Seconds+")";
                    recording.Stop();
                    var recordedFile = recording.ToFile();
                    recording.Dispose();
                    recordedFile.Write(playlistPath+file);
                    recording = null;
                    Console.Out.WriteLine(timing);
                    break;
            }
            state = State.idle;
        }
        public void Pause() { _playback.Stop(); }
        public void Resume() { _playback.Start(); }
        public string file;
        public string description;
        public string timing;
        public string doc;
        public string performer;
    }
 }
