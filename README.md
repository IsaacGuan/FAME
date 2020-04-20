# FAME: Functionality-Aware Model Evolution

This repository contains the code for the paper "3D Shape Generation via Functionality-Aware Model Evolution".

## Data

The pre-processed models are excluded from the `data` folder in this repository. To get the data for the experiment, please contact either [me](https://isaacguan.github.io/) or my supervisor [Dr. Oliver van Kaick](https://people.scs.carleton.ca/~olivervankaick/).

## MATLAB Code

Please download the MATLAB code [here](https://drive.google.com/open?id=1QnnHsxcZiDPj8MMinUBvU6FkZCnsrZxN) of the category functionality models for functionality partial matching, and extract the `PartialMatching` folder to the same directory where this repository resides.

## Running the Code

This project is compiled and built using [Visual Studio](https://visualstudio.microsoft.com/). To run the code, please open `fameBase.sln` in Visual Studio and run the project with `x86` platform. To load the data, click on "Model" from the menu bar and select "Load models" in the drop-down menu. Then, select the folder containing the pre-processed models, e.g., `set_1`. After the models are loaded, select the functionality labels from the checkboxes in the bottom and click on the button "Run".

After the evolution is finished, results are saved in the `Users` folder under the folder of the pre-processed models, e.g., `set_1/Users/`.
