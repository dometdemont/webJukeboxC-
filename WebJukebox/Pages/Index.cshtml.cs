using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;

namespace WebJukebox.Pages
{
    public class IndexModel : PageModel
    {
        public string Message { get; private set; } = "PageModel in C#";
        private static Title[] playList = {
            new Title("LISZT.MID", "Franz Liszt : Prélude et Fugue sur B.A.C.H. (10')", "15, 617, 20"),
            new Title("MESSIAEN.MID", "Olivier Messiaen : Banquet céleste (6')", "5, 375, 19"),
            new Title("ASCENSIO.MID", "Olivier Messiaen : Prière du Christ montant vers son Père (7')", "8, 425, 10"),
            new Title("WAGNER.MID", "Richard Wagner : Mort d'Isolde (8'40)", "8, 520, 20"),
            new Title("DUPRE.MID", "Marcel Dupré : Prélude et Fugue en sol mineur (7')", "12, 403, 10"),
            new Title("COUPERIN.MID", "François Couperin : Tierce en taille (4')", "6, 240, 20"),
            new Title("FRANCK.MID", "César Franck : Troisième Choral (10'20)", "13, 622, 20"),
            new Title("CHROMORN.MID", "François Couperin : Chromorne en taille (4')", "7, 233, 20"),
            new Title("TOCAREM.MID", "J.S. Bach: Toccata en Ré mineur (2'30)", "2, 145, 5"),
            new Title("LANGLAIS.MID", "Jean Langlais : Chant de Paix (2'30)", "6, 150, 4"),
            new Title("GUILMANT.MID", "Alexandre Guilmant : Noël 'Or dites-nous Marie' (2'20)", "6, 130, 4")
        };

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
            for (int i = 0; i < playList.Length; i++)
            {
                Message += "<li><a href=" + i + ">" + playList[i].description + "</a></li>";
            };
            Message += "</ul>";
            Message += "<table><tr>";
            Message += "<td align=right><a href='/'> Actualiser</a></td>";
            Message += "</tr></table>";
        }
        private void Current()
        {
            Message = "<h3>Pièce en cours d'audition</h3><p>" + currentTitle.description + "</p>";
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
                    _logger.LogInformation("onGet invoked with id: {int}", id);
                    currentTitle = playList[(int)id];
                    currentTitle.Start();
                    Current();            
                    CountDown();
                } else if(id == stopId) { 
                    currentTitle.Cancel();
                    currentTitle = null;
                    Welcome();
                }
                else if (id == pauseId)
                {
                    currentTitle.Pause();
                    Current();
                }
                else if (id == resumeId)
                {
                    currentTitle.Resume();
                    Current();
                    CountDown();
                }
            }
            else
            {
                if (Title.IsFree())
                {
                    Welcome();
                }
                else
                {
                    Current();
                    if (Title.IsPlaying()) CountDown(); 
                }
            }
        }
    }

    class Title
    {
        private static Playback _playback;
        private static OutputDevice outputDevice;
        private static bool free = true;
        private string playlistPath = "C:/Users/domin/Desktop/Jukebox/";
        private string midiOutputDevice = "Microsoft GS Wavetable Synth";//"USB MIDI Interface";

        public Title(string aFile, string aDescription, string aTiming)
        {
            file = aFile;
            description = aDescription;
            timing = aTiming;
        }
        public static bool IsPlaying() { return _playback != null && _playback.IsRunning; }
        public static bool IsFree() { return free; }

        public static double GetElapsed() {
            if (_playback == null) return 0;
            MetricTimeSpan result = (MetricTimeSpan)_playback.GetCurrentTime(TimeSpanType.Metric);
            return result.TotalSeconds;
        }
        public void Start() {
            var midiFile = MidiFile.Read(playlistPath+file);

            outputDevice = OutputDevice.GetByName(midiOutputDevice);

            _playback = midiFile.GetPlayback(outputDevice);
            _playback.Start();
            _playback.Finished += OnFinished;
            free = false;
        }
        private static void OnFinished(object sender, EventArgs e)
        {
            _playback.Dispose();
            outputDevice.Dispose();
            free = true;
        }
        public void Cancel() {
            if (_playback.IsRunning)
            {
                _playback.Stop();
                outputDevice.Dispose();
                _playback.Dispose();
            }
            free = true;
        }
        public void Pause() { _playback.Stop(); }
        public void Resume() { _playback.Start(); }
        public string file;
        public string description;
        public string timing;
    };

 }