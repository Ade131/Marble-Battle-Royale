import pandas as pd
import chart_studio.plotly as py
import plotly.graph_objs as go
import glob
import os

#themes
#simple_white
#plotly_white
#plotly_dark

def SearchFiles(word=""):
	files = []
	for file in glob.glob("*"):
		if file.endswith(word):
			files.append(file)
	return files

for csvFileName in SearchFiles(".csv"):
	htmlFileName = csvFileName.replace(".csv", ".html")
	if os.path.exists(htmlFileName):
		print("Skipping " + csvFileName + ", HTML file already exists...")
		continue

	df = pd.read_csv(csvFileName, comment='#')
	df.head()

	print("Generating HTML file for " + csvFileName)

	graphs = []

	for col in df.columns:
		if col == df.columns[0]:
			continue
		graphs.append(go.Scatter(x=df[df.columns[0]], y=df[col], mode="lines", name=col))

	layout = go.Layout(title=csvFileName, template="plotly_white")
	layout.xaxis.title=df.columns[0]
	layout.yaxis.title="Value"

	fig = go.Figure(data=graphs, layout=layout)
	fig.write_html(htmlFileName)
#	fig.show()

for logFileName in SearchFiles(".log"):
	htmlFileName = logFileName.replace(".log", ".html")
	if os.path.exists(htmlFileName):
		print("Skipping " + logFileName + ", HTML file already exists...")
		continue

	df = pd.read_csv(logFileName, comment='#')
	df.head()

	print("Generating HTML file for " + logFileName)

	graphs = []

	for col in df.columns:
		if col == df.columns[0]:
			continue
		graphs.append(go.Scatter(x=df[df.columns[0]], y=df[col], mode="lines", name=col))

	layout = go.Layout(title=logFileName, template="plotly_white")
	layout.xaxis.title=df.columns[0]
	layout.yaxis.title="Value"

	fig = go.Figure(data=graphs, layout=layout)
	fig.write_html(htmlFileName)
#	fig.show()
