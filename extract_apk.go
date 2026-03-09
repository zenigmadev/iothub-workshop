package main

import (
	"archive/tar"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
)

func main() {
	if len(os.Args) < 3 {
		fmt.Fprintf(os.Stderr, "Usage: %s <apk-file> <output-dir>\n", os.Args[0])
		os.Exit(1)
	}

	apkFile := os.Args[1]
	outputDir := os.Args[2]

	if err := os.MkdirAll(outputDir, 0755); err != nil {
		fmt.Fprintf(os.Stderr, "Error creating output directory: %v\n", err)
		os.Exit(1)
	}

	f, err := os.Open(apkFile)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error opening APK file: %v\n", err)
		os.Exit(1)
	}
	defer f.Close()

	gzr, err := gzip.NewReader(f)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error creating gzip reader: %v\n", err)
		os.Exit(1)
	}
	defer gzr.Close()

	tr := tar.NewReader(gzr)

	for {
		header, err := tr.Next()
		if err == io.EOF {
			break
		}
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error reading tar: %v\n", err)
			os.Exit(1)
		}

		if !strings.HasPrefix(header.Name, "usr/lib/") && !strings.HasPrefix(header.Name, "lib/") {
			continue
		}

		target := filepath.Join(outputDir, header.Name)

		switch header.Typeflag {
		case tar.TypeDir:
			if err := os.MkdirAll(target, 0755); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating directory: %v\n", err)
				os.Exit(1)
			}
		case tar.TypeReg:
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating parent directory: %v\n", err)
				os.Exit(1)
			}
			outFile, err := os.OpenFile(target, os.O_CREATE|os.O_WRONLY, os.FileMode(header.Mode))
			if err != nil {
				fmt.Fprintf(os.Stderr, "Error creating file: %v\n", err)
				os.Exit(1)
			}
			if _, err := io.Copy(outFile, tr); err != nil {
				outFile.Close()
				fmt.Fprintf(os.Stderr, "Error writing file: %v\n", err)
				os.Exit(1)
			}
			outFile.Close()
			fmt.Printf("Extracted: %s\n", target)
		case tar.TypeSymlink:
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating parent directory: %v\n", err)
				os.Exit(1)
			}
			if err := os.Symlink(header.Linkname, target); err != nil {
				fmt.Fprintf(os.Stderr, "Warning: symlink error: %v\n", err)
			} else {
				fmt.Printf("Created symlink: %s -> %s\n", target, header.Linkname)
			}
		}
	}
	fmt.Println("Extraction complete")
}
