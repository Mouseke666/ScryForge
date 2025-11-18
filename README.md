# ScryForge

ScryForge is an automated pipeline for downloading, upscaling, and processing Magic: The Gathering cards. It generates PDF files from your cards and supportsflip cards.

## Features

- Automatically download card art via an external downloader.
- Upscale cards for high-quality output.
- Supports double-faced and flip cards.
- Copies, duplicates, and organizes card files.
- Generates PDF files (`default.pdf` and `flips.pdf`) from the cards.
- Automatic cleanup of temporary and upscaled files.
- Easily opens the output folder after processing.
- Console logging with timestamps and clear status updates.

### Preparing `cards.txt`

To use ScryForge, open your deck in Moxfield and export it as a text file. Copy the exported list into `cards.txt`. The pipeline will read this file to download and process your cards.

## Project Structure

- **Services**
  - `CleanupService` – Removes files and folders that are no longer needed.
  - `OpenFolderService` – Opens a folder in Windows Explorer.
  - `CardParserService` – Parses `cards.txt` and extracts card information.
  - `DownloaderService` – Starts the art downloader and waits for it to finish.
  - `UpscalerService` – Upscales the downloaded card images.
  - `CopyService` – Copies and duplicates card files.
  - `FlipService` – Processes double-faced flip cards.
  - `PDFService` – Generates PDF files from the cards.
  - `PDFOpenService` – (optional) opens PDF files.
  - `PipelineService` – Coordinates all pipeline steps in the correct order.

- **Models**
  - `CardInfo` – Holds information about a card (name, set, quantity, front/back file).

- **Configuration**
  - `AppConfig` – Contains paths and file locations for cards, downloads, and PDF output.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/ScryForge.git
   cd ScryForge
