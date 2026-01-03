# ScryForge

ScryForge is an automated pipeline for downloading, upscaling, and processing Magic: The Gathering cards. It generates PDF files from your cards and fully supports double-faced and flip cards, including custom cards.

## Features

- Automatically download card art via an external downloader.
- Upscale cards for high-quality output (only if cards are present).
- Supports double-faced and flip cards.
- Handles custom cards from a `Custom` folder.
- Copies, duplicates, and organizes card files.
  - Custom cards are copied directly to the output folders.
  - Flip cards are copied to a dedicated `flips` folder.
- Generates PDF files (`default.pdf` and `flips.pdf`) from the cards.
- Automatic cleanup of temporary and upscaled files.
- Easily opens the output folder after processing.
- Console logging with timestamps and clear status updates.

### Preparing `cards.txt`

To use ScryForge, open your deck in Moxfield and export it as a text file. Copy the exported list into `cards.txt`. The pipeline will read this file to download and process your cards.

### Custom Cards

- Place any custom card images (`.jpg` or `.png`) in the `Custom` folder in the application directory.
- Flip cards can have a back image with the prefix `__back_`.
  - **Important:** The filename after `__back_` must **exactly match** the corresponding front card filename, including the extension.
  - ScryForge will automatically pair front and back images based on this naming convention.

## Upscaler Configuration

ScryForge allows full control over the settings of the upscaler for your cards. The configuration is located in `appsettings.json` under the `Upscaler` section and supports the following options:

```json
{
  "Upscaler": {
    "Model": "digital-art-4x",
    "Scale": 4,
    "Threads": 6
  }
}
```

### Fields

- **Model** – The upscaler model to use (e.g., `digital-art-4x`).
- **Scale** – The resolution multiplier (e.g., `2` or `4`).
- **Threads** – The number of CPU threads the upscaler can use. This allows you to adjust performance depending on your system. By default, ScryForge uses all available cores.

### Tips

- Increase the number of threads on multi-core systems for faster processing.  
- Reduce the number of threads on lighter systems to limit CPU usage.

## Project Structure

- **Services**
  - `CleanupService` – Removes files and folders that are no longer needed.
  - `OpenFolderService` – Opens a folder in Windows Explorer.
  - `CardParserService` – Parses `cards.txt` and custom cards into internal card info format.
  - `DownloaderService` – Starts the art downloader and waits for it to finish.
  - `UpscalerService` – Upscales the downloaded card images (skipped if no cards present).
  - `CustomCardService` – Loads and copies custom card images to the output folders.
  - `CopyService` – Copies and duplicates card files for PDF generation.
  - `FlipService` – Processes double-faced flip cards (skips custom cards).
  - `PDFService` – Generates PDF files from the cards.
  - `PDFOpenService` – (optional) opens PDF files.
  - `PipelineService` – Coordinates all pipeline steps in the correct order.

- **Models**
  - `CardInfo` – Holds information about a card (name, set, quantity, front/back file).
  - `CustomCard` – Represents a custom card with optional back image.

- **Configuration**
  - `AppConfig` – Contains paths and file locations for cards, downloads, custom cards, flips, and PDF output.

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/ScryForge.git
   cd ScryForge
