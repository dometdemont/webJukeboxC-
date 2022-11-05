using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;

namespace WebJukebox.Pages.Shared
{
    // The implementation of a score recorded as a video
    class Score
    {
        public string? video;
        public int speed;
        public int offset;

        public Score(string? video, int speed, int offset)
        {
            this.video = video;
            this.speed = speed;
            this.offset = offset;
        }
    }
    // The implementation of a title to play / record / forward
    class Title
    {
        private static Playback _playback;
        private static OutputDevice outputDevice;
        private static InputDevice inputDevice;
        enum State
        {
            idle,
            playing,
            recording,
            forwarding
        }
        // The pipe organ manages 5 MIDI channels: one per key/pedal board, one for control messages
        // Swell and Positif have expression capability
        enum PipeOrganChannel
        {
            Great = 0,
            Pedal = 1,
            Swell = 2,
            Positif = 3,
            Control = 15
        }
        private static FourBitNumber getPipeOrganChannel(PipeOrganChannel c)
        {
            return (FourBitNumber)(int)c;
        }
        private static State state = State.idle;
        private static string playlistPath;
        private static string lastError;
        private static Timer heartBeat;
        private static Recording recording;
        private static MetricTimeSpan firstRecordedNoteTime, lastRecordedNoteTime;
        private static DevicesConnector devicesConnector;
        // Two implementations for the forwarding/filtering function: with DevicesConnector or InputDevice+OutputDevice
        // UseDevicesConnector false recommended to forward only events of interest
        private static bool UseDevicesConnector = false;
        private static int lastForwardedProgramChange,
            lastForwardedExpressionPositif, lastForwardedExpressionSwell,
            activeForwardedNotes, totalForwardedNotes;

        // Registration description for each program on # 19
        private static string[] registration19 =
        {
            "(Rien)",
            "Bourdon 8",
            "Montre 8",
            "Fonds 8",
            "Fonds 8, 4",
            "Fonds 8, 4, 2",
            "Fonds 8 GO Solo baryton",
            "Voix céleste",
            "Clarinette",
            "Bourdon Solo cornet Récit",
            "Bourdon Solo clarinette Récit",
            "Basson 16 en solo sur Fonds 8 GO",
            "Quintaton 16",
            "Cornet",
            "Trompette Clairon",
            "Tutti",
            "Imitation musette: Voix céleste, clarinette, Quintaton, Flûte creuse"
        };

        public Title(string aFile, string aDescription, string aTiming, string? aPerformer, int aSpeed, string? aDoc, Score? aScore)
        {
            // The MIDI file to playback
            file = aFile;
            // One line description of the MIDI file
            description = aDescription;
            // String of 3 comma separated periods expressed in seconds: loading, playing, unloading times 
            timing = aTiming;
            // Optional URL displayed during the play
            doc = aDoc;
            // Optional URL displaying the score
            score = aScore;
            // String displayed as the MIDI recording performer
            performer = aPerformer;
            // Playback speed in %, ie 100=normal speed, 50 half speed, etc.
            speed = aSpeed;
        }
        public static string getLastError() { string err = lastError; lastError = null; return err; }
        public static bool IsPlaying() { return _playback != null && _playback.IsRunning; }
        public static bool IsFree() { return state == State.idle; }
        public static bool IsRecording() { return state == State.recording; }
        public static bool IsForwarding() { return state == State.forwarding; }
        public static void SetMidiDevice(OutputDevice d)
        {
            if (outputDevice != null) outputDevice.Dispose();
            outputDevice = d;
        }
        public static void SetMidiInputDevice(InputDevice d)
        {
            if (inputDevice != null) inputDevice.Dispose();
            inputDevice = d;
        }
        public static void SetCatalogPath(string p) { playlistPath = p; }
        public static double GetElapsed(int speed)
        {
            if (_playback == null) return 0;
            MetricTimeSpan result = (MetricTimeSpan)_playback.GetCurrentTime(TimeSpanType.Metric);
            return result.TotalSeconds*100/speed;
        }
        public static void Heartbeat(object? state)
        {
            if(outputDevice != null)try
            {
                // Send a void command to keep the device alive: open a fake swell box
                ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
                swellOn.Channel = getPipeOrganChannel(PipeOrganChannel.Great); outputDevice.SendEvent(swellOn);
            }
            catch (Exception e) { Console.Out.WriteLine("Cannot send heartbeat: " + e.Message); };
        }
        public static void SwellOn()
        {
            // Open the swell boxes
            ControlChangeEvent swellOn = new((SevenBitNumber)11, (SevenBitNumber)100);
            swellOn.Channel = getPipeOrganChannel(PipeOrganChannel.Positif); outputDevice.SendEvent(swellOn);
            swellOn.Channel = getPipeOrganChannel(PipeOrganChannel.Swell); outputDevice.SendEvent(swellOn);
        }
        // Start the MIDI Forward mode including event transformation for Korg and Yamaha EZ
        public static Title Forward()
        {
            SwellOn();
            lastForwardedExpressionPositif = lastForwardedExpressionSwell = 100;
            lastForwardedProgramChange = 0;
            activeForwardedNotes = 0; totalForwardedNotes = 0;
            if (UseDevicesConnector)
            {
                devicesConnector = new DevicesConnector(inputDevice, outputDevice);
                devicesConnector.Connect();
                devicesConnector.EventCallback += OnForwardEventReceived;
            }
            else
            {
                inputDevice.EventReceived += OnInputEventReceived;
            }
            inputDevice.StartEventsListening();
            state = State.forwarding;
            return new Title(
                "EMPTY.MID",
                "Forwarded " + DateTime.Now.ToString(),
                "1,2,1", null, 100, null, null);
        }
        // Start a new recording on the input device
        // Name the file using the current date and time to make it unique
        public static Title Record()
        {
            firstRecordedNoteTime = null;
            lastRecordedNoteTime = null;
            recording = new Recording(TempoMap.Default, inputDevice);
            inputDevice.EventReceived += OnRecordingEventReceived;
            inputDevice.StartEventsListening();
            recording.Start();
            state = State.recording;
            return new Title(
                "Recorded" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".mid",
                "Enregistrement " + DateTime.Now.ToString(),
                "1,2,1", null, 100, null, null);
        }
        public static String getRecordingInfo()
        {
            if (recording == null) return null;
            String firstNote = "Aucune note reçue";
            String lastNote = "Aucune note terminée";
            if (firstRecordedNoteTime != null)
            {
                firstNote = "1ère note : " + firstRecordedNoteTime.TotalSeconds;
            }
            if (lastRecordedNoteTime != null)
            {
                lastNote = "Dernière note : " + lastRecordedNoteTime.TotalSeconds;
            }
            return "<p>IsRunning: " + recording.IsRunning + "</p><p>IsListening: " + inputDevice.IsListeningForEvents +
                "</p><p>Durée: " + recording.GetDuration(TimeSpanType.Metric).ToString() +
                "</p><p>" + firstNote + "</p>" +
                "</p><p>" + lastNote + "</p>";
        }

        public static String getForwardedInfo()
        {
            String stopsOn19 = "?";
            if(lastForwardedProgramChange < registration19.Length)stopsOn19=registration19[lastForwardedProgramChange];
            return "<table><tr><td>Combinaison</td><td>" + lastForwardedProgramChange +
                "</td></tr><tr><td>Jeux : "+ stopsOn19 +
                "</td></tr><tr><td>Expression Récit</td><td>" + lastForwardedExpressionSwell +
                "</td></tr><tr><td>Expression Positif</td><td>" + lastForwardedExpressionPositif +
                "</td></tr><tr><td>Nombre de notes actives</td><td>" + activeForwardedNotes +
                "</td></tr><tr><td>Nombre total de notes</td><td>" + totalForwardedNotes +
                "</td><td></table>";
        }
        private static MidiEvent BuildExpression(SevenBitNumber pitch)
        {
            MidiEvent result = null;
            // Special notes driving the expressions
            // 12 notes with pitch from 24 to 35 are reserved for swell and positif expression with 6 levels each: 0, 20, 40, 60, 80, 100%
            // 24-29: swell from do to fa
            // 30-35: positif from fa# to si
            if (pitch < 36)
            {
                int outChannel = getPipeOrganChannel(PipeOrganChannel.Swell) + (pitch - 24) / 6;
                int outExpr = (pitch - 24) % 6 * 20;
                ControlChangeEvent ex = new ControlChangeEvent((SevenBitNumber)11, (SevenBitNumber)outExpr);
                ex.Channel = (FourBitNumber)outChannel;
                result = ex;
                Console.Out.WriteLine("Expression on channel " + outChannel + ":" + outExpr);
                if (ex.Channel == getPipeOrganChannel(PipeOrganChannel.Swell)) lastForwardedExpressionSwell = outExpr;
                if (ex.Channel == getPipeOrganChannel(PipeOrganChannel.Positif)) lastForwardedExpressionPositif = outExpr;
            }
            return result;
        }

        // Apply event transformation when forwarding
        // Two implementations for the forwarding/filtering feature: using explicit inputDevice and outputDevice or devicesConnector
        private static void OnInputEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            MidiEvent outEvent = OnForwardEventReceived(e.Event);
            if (outEvent != null) outputDevice.SendEvent(outEvent);
        }
        private static MidiEvent OnForwardEventReceived(MidiEvent e)
        {
            MidiEvent result = null;
            switch (e.EventType)
            {
                case MidiEventType.NoteOn:
                    NoteOnEvent on = (NoteOnEvent)e;
                    result = BuildExpression(on.NoteNumber);
                    if (result == null)
                    // Normal notes forced to Great keyboard channel 
                    {
                        on.Channel = getPipeOrganChannel(PipeOrganChannel.Great);
                        result = on;
                        // Note on with null velocity has to be considered as note off as per the MIDI spec
                        if (on.Velocity > 0) activeForwardedNotes++;
                        else { activeForwardedNotes--; totalForwardedNotes++; }
                    }
                    break;
                case MidiEventType.NoteOff:
                    NoteOffEvent off = (NoteOffEvent)e;
                    result = BuildExpression(off.NoteNumber);
                    if (result == null)
                    // Normal notes forced to Great keyboard channel 
                    {
                        off.Channel = getPipeOrganChannel(PipeOrganChannel.Great);
                        result = off;
                        activeForwardedNotes--;
                        totalForwardedNotes++;
                    }
                    break;

                case MidiEventType.ProgramChange:
                    // Send programm changes to control channel
                    // Map them consistently with the pipe organ expectations
                    int c;
                    ProgramChangeEvent output = new ProgramChangeEvent();
                    ProgramChangeEvent input = (ProgramChangeEvent)e;
                    // Ignore programm change except on the first channel 
                    if (input.Channel > 0) return result;
                    switch (input.ProgramNumber)
                    {
                        case 24: c = 2; break;
                        case 25: c = 1; break;
                        case 26: c = 3; break;
                        case 27: c = 4; break;
                        case 28: c = 5; break;
                        case 29: c = 6; break;
                        case 30: c = 7; break;
                        case 32: c = 8; break;
                        case 33: c = 9; break;
                        case 34: c = 10; break;
                        case 35: c = 11; break;
                        case 36: c = 12; break;
                        case 38: c = 13; break;
                        case 39: c = 14; break;
                        case 105: c = 15; break;
                        case 106: c = 16; break;
                        default: c = input.ProgramNumber; break;
                    }
                    output.Channel = getPipeOrganChannel(PipeOrganChannel.Control);
                    output.ProgramNumber = (SevenBitNumber)c;
                    result = output;
                    Console.Out.WriteLine("Programm change " + c);
                    lastForwardedProgramChange = c;
                    break;
            }
            return result;
        }
        // Record specific events timing: first note, last note
        private static void OnRecordingEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            if (e.Event.EventType == MidiEventType.NoteOn)
            {
                if (firstRecordedNoteTime == null) firstRecordedNoteTime = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
                lastRecordedNoteTime = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
            }
        }

        // Start a playback
        public void Start()
        {
            if (heartBeat != null) heartBeat.Dispose();
            SwellOn();
            var midiFile = MidiFile.Read(playlistPath + file);
            _playback = midiFile.GetPlayback(outputDevice);
            if(speed != null)_playback.Speed = (float)speed/100;
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
        public void Cancel()
        {
            switch (state)
            {
                case State.playing:
                    heartBeat = new Timer(Heartbeat, null, 10000, 1000);
                    _playback.Stop();
                    _playback.Dispose();
                    break;
                case State.recording:
                    MetricTimeSpan end = (MetricTimeSpan)recording.GetDuration(TimeSpanType.Metric);
                    if (firstRecordedNoteTime != null && lastRecordedNoteTime != null) timing =
                        (int)firstRecordedNoteTime.TotalSeconds +
                        "," + (int)(lastRecordedNoteTime.TotalSeconds - firstRecordedNoteTime.TotalSeconds) +
                        "," + (int)(end.TotalSeconds - lastRecordedNoteTime.TotalSeconds);
                    description += " (" + end.Minutes + "'" + end.Seconds + ")";
                    recording.Stop();
                    var recordedFile = recording.ToFile();
                    recording.Dispose();
                    recordedFile.Write(playlistPath + file);
                    recording = null;
                    Console.Out.WriteLine(timing);
                    break;
                case State.forwarding:
                    if (UseDevicesConnector) devicesConnector.Disconnect();
                    inputDevice.Dispose();
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
        public Score score;
        public string performer;
        public int speed;
    }
}
