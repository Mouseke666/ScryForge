ScryForge

ScryForge is an automated pipeline for downloading, upscaling, and processing Magic: The Gathering cards. It generates PDF files from your cards and supports double-faced and flip cards.

Features

Automatically download card art via an external downloader.

Upscale cards for high-quality output.

Supports double-faced and flip cards.

Copies, duplicates, and organizes card files.

Generates PDF files (default.pdf and flips.pdf) from the cards.

Automatic cleanup of temporary and upscaled files.

Easily opens the output folder after processing.

Console logging with timestamps and clear status updates.

Project Structure

Services

CleanupService – Removes files and folders that are no longer needed.

OpenFolderService – Opens a folder in Windows Explorer.

CardParserService – Parses cards.txt and extracts card information.

DownloaderService – Starts the art downloader and waits for it to finish.

UpscalerService – Upscales the downloaded card images.

CopyService – Copies and duplicates card files.

FlipService – Processes double-faced flip cards.

PDFService – Generates PDF files from the cards.

PDFOpenService – (optional) opens PDF files.

PipelineService – Coordinates all pipeline steps in the correct order.

Models

CardInfo – Holds information about a card (name, set, quantity, front/back file).

Configuration

AppConfig – Contains paths and file locations for cards, downloads, and PDF output.

Installation

Clone the repository:

git clone https://github.com/mouseke666/ScryForge.git
cd ScryForge


Make sure the following files are available:

cards.txt – Text file with your card list.

Art downloader executable (e.g., MTG Art Downloader.exe) at the path specified in AppConfig.

Upscaler tool at the path specified in AppConfig.

PDF generator executable at the path specified in AppConfig.

Build and run the project via the console:

dotnet build
dotnet run

Usage

Place your cards.txt in the specified folder (AppConfig.BasePath).

Start the application:

dotnet run


The pipeline will execute the following steps:

Clean up existing downloads and PDF files.

Download card art.

Upscale the images.

Parse cards from cards.txt.

Duplicate cards if needed.

Process flip cards.

Generate default.pdf.

Clean up upscaled files.

Move PDF to the root folder.

Generate flips.pdf if flip cards are present.

Upon completion, the output folder will automatically open.

Logging

The pipeline uses console logging with timestamps:

HH:mm:ss [INFO] Pipeline started
HH:mm:ss [INFO] Step 1/10 – Cleaning up directories...
...

Contributing

Contributions are welcome! Fork the repository, make your changes, and submit a pull request.

License

MIT License
