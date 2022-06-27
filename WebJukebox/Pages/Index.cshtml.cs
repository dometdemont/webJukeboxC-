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
        private static readonly string d3m = "Dominique Domet de Mont";
        private static readonly string lml = "Louis-Marie Lardic";
        private static Title[] playList = {
            new Title("LISZT.MID", "Franz Liszt : Prélude et Fugue sur B.A.C.H. (10')", "15, 617, 20", "https://fr.wikipedia.org/wiki/Fantasie_und_Fuge_%C3%BCber_das_Thema_B-A-C-H#p-lang-btn", lml),
            new Title("MESSIAEN.MID", "Olivier Messiaen : Banquet céleste (6')", "5, 375, 19", "https://en.wikipedia.org/wiki/Le_Banquet_C%C3%A9leste#p-lang-btn", d3m),
            new Title("ASCENSIO.MID", "Olivier Messiaen : Prière du Christ montant vers son Père (7')", "8, 425, 10", "https://fr.wikipedia.org/wiki/Olivier_Messiaen#p-lang-btn", d3m),
            new Title("WAGNER.MID", "Richard Wagner : Mort de Isolde, transcription (8'40)", "8, 510, 2", "https://fr.wikipedia.org/wiki/Liebestod#p-lang-btn", lml),
            new Title("DUPRE.MID", "Marcel Dupré : Prélude et Fugue en sol mineur (7')", "12, 403, 10", "https://fr.wikipedia.org/wiki/Marcel_Dupr%C3%A9#p-lang-btn", lml),
            new Title("COUPERIN.MID", "François Couperin : Tierce en taille (4')", "6, 240, 2", "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", d3m),
            new Title("FRANCK.MID", "César Franck : Troisième Choral (10'20)", "13, 622, 20", "https://fr.wikipedia.org/wiki/C%C3%A9sar_Franck#p-lang-btn", d3m),
            new Title("CHROMORN.MID", "François Couperin : Chromorne en taille (4')", "5, 233, 20", "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", d3m),
            new Title("TOCAREM.MID", "J.S. Bach: Toccata en Ré mineur (2'30)", "2, 145, 5", "https://fr.wikipedia.org/wiki/Toccata_et_fugue_en_r%C3%A9_mineur#p-lang-btn", d3m),
            new Title("LANGLAIS.MID", "Jean Langlais : Chant de Paix (2'30)", "4, 150, 4", "https://fr.wikipedia.org/wiki/Jean_Langlais#p-lang-btn", d3m),
            new Title("GUILMANT.MID", "Alexandre Guilmant : Noël 'Or dites-nous Marie' (2'20)", "6, 130, 4", "https://fr.wikipedia.org/wiki/Alexandre_Guilmant#p-lang-btn", d3m)
        };
        private static string CatalogPath="C:/Users/domin/Desktop/Jukebox/";

        private static int stopId = playList.Length;
        private static int pauseId = stopId+1;
        private static int resumeId = pauseId+1;


        private readonly ILogger<IndexModel> _logger;

        private static Title? currentTitle =null;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        private void Welcome()
        {
            Message = "<h3>Liste des pièces disponibles</h3><ul>";
            string err = Title.getLastError();
            if (err != null) Message += "<p>Error: "+err+"</p>";
            for (int i = 0; i < playList.Length; i++)
            {
                Message += "<li><a href=" + i + ">" + playList[i].description + "</a></li>";
            };
            Message += "</ul>";
            Message += "<table><tr>";
            if(OutputDevice.GetDevicesCount() > 0)
            {
                OutputDevice lastMidiDevice = OutputDevice.GetByIndex(OutputDevice.GetDevicesCount() - 1);
                Title.SetMidiDevice(lastMidiDevice);
                Title.SetCatalogPath(CatalogPath);
                Message += @"<td align=left><ul>";
                Message += "<li>Périphérique de sortie MIDI : " + lastMidiDevice.Name;
                Message += "<li>Répertoire du catalogue : " + CatalogPath;
                Message += "</ul></td>";
                Message += "<td align=right><a href='/'> Actualiser</a></td>";
                Message += "</tr></table>";
            } else
            {
                Message = "<h3>Aucun périphérique de sortie MIDI détecté/h3>";
            }
            
        }
        private void Current(bool showCountDown)
        {
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
                    currentTitle.Cancel();
                    currentTitle = null;
                    Welcome();
                }
                else if (id == pauseId)
                {
                    _logger.LogInformation("Pausing id: {int}", id);
                    currentTitle.Pause();
                    Current(false);
                }
                else if (id == resumeId)
                {
                    _logger.LogInformation("Resuming id: {int}", id);
                    currentTitle.Resume();
                    Current(true);
                }
            }
            else
            {
                if (Title.IsFree())
                {
                    _logger.LogInformation("Displaying Welcome");
                    Welcome();
                }
                else
                {
                    _logger.LogInformation("Displaying current play");
                    Current(Title.IsPlaying());
                }
            }
        }
    }

    class Title
    {
        private static Playback _playback;
        private static OutputDevice outputDevice;
        private static bool free = true;
        private static string playlistPath;
        private static string lastError;
        private static Timer heartBeat;

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
        public static bool IsFree() { return free; }
        public static void SetMidiDevice(OutputDevice d) {
            if (outputDevice != null) outputDevice.Dispose();
            outputDevice = d; }
        public static void SetCatalogPath(string p) { playlistPath = p; }
        public static double GetElapsed() {
            if (_playback == null) return 0;
            MetricTimeSpan result = (MetricTimeSpan)_playback.GetCurrentTime(TimeSpanType.Metric);
            return result.TotalSeconds;
        }
        public static void Heartbeat(object? state)
        {
            // Send a void command to keep the device alive: oOpen a fake swell box
            ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
            swellOn.Channel = (FourBitNumber)0; outputDevice.SendEvent(swellOn);
        }
        public static void SwellOn()
        {
            // Open the swell boxes
            ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
            swellOn.Channel = (FourBitNumber)3; outputDevice.SendEvent(swellOn);
            swellOn.Channel = (FourBitNumber)2; outputDevice.SendEvent(swellOn);
        }

        public void Start() {
            if (heartBeat != null) heartBeat.Dispose();
            SwellOn();
            var midiFile = MidiFile.Read(playlistPath+file);
            _playback = midiFile.GetPlayback(outputDevice);
            _playback.DeviceErrorOccurred += OnError;
            _playback.Start();
            _playback.Finished += OnFinished;
            free = false;
    }
        private static void OnError(object sender, EventArgs e)
        {
            lastError = e.ToString();
        }
        private static void OnFinished(object sender, EventArgs e)
        {
            heartBeat = new Timer(Heartbeat, null, 0, 1000); 
            _playback.Dispose();
            free = true;
        }

       
        public void Cancel() {
            if (!free)
            {
                heartBeat = new Timer(Heartbeat, null, 0, 1000);
                _playback.Stop();
                _playback.Dispose();
            }
            free = true;
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