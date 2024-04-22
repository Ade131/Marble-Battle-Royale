Steps to generate & analyze recordings
========================================
1) Add any Recorder script on your game object.
2) Configure recorder type. There are three types available:
    A) Frame           - X axis in graph is a linear representation of Time.frameCount - file ends with (Frame).
    B) Engine Time     - X axis in graph is a linear representation of Time.unscaledTime - file ends with (EngineTime).
    C) Monitor Time    - X axis in graph is a linear representation of simulated monitor time - file ends with (MonitorTime).
    D) Simulation Tick - X axis in graph is a linear representation of Runner.Tick - file ends with (SimulationTick).
3) Start playing in editor / run a build.
4) Start recording by calling Recorder.SetActive(true) - typically on your local player instance.
5) Stop recording by calling Recorder.SetActive(false) / quit the game.
6) A file is generated for each Recorder in the project root folder (Editor) or in the build folder.
7) Run CreateHTMLGraphs.py from Explorer - this script generates browsable graphs (html + Plotly) from *.csv and *.log files.
   The script processes only files in the same folder, you'll need to copy it.
   Python packages required: pandas, plotly, chart_studio.
8) Open generated HTML files and check recorded values.
9) Enjoy debugging!
