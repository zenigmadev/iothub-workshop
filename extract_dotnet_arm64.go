package main

import (
	"archive/tar"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"path/filepath"
)

func main() {
	tarFile := "/tmp/dotnet-sdk-musl-arm64.tar.gz"
	destDir := os.Getenv("HOME") + "/dotnet-musl"
	
	// Remove old directory if exists
	os.RemoveAll(destDir)
	
	// Create destination directory
	if err := os.MkdirAll(destDir, 0755); err != nil {
		fmt.Fprintf(os.Stderr, "Error creating directory: %v\n", err)
		os.Exit(1)
	}
	
	// Open tar.gz file
	f, err := os.Open(tarFile)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error opening tar file: %v\n", err)
		os.Exit(1)
	}
	defer f.Close()
	
	// Create gzip reader
	gzr, err := gzip.NewReader(f)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error creating gzip reader: %v\n", err)
		os.Exit(1)
	}
	defer gzr.Close()
	
	// Create tar reader
	tr := tar.NewReader(gzr)
	
	// Extract files
	for {
		header, err := tr.Next()
		if err == io.EOF {
			break
		}
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error reading tar: %v\n", err)
			os.Exit(1)
		}
		
		target := filepath.Join(destDir, header.Name)
		
		switch header.Typeflag {
		case tar.TypeDir:
			if err := os.MkdirAll(target, os.FileMode(header.Mode)); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating directory %s: %v\n", target, err)
				os.Exit(1)
			}
		case tar.TypeReg:
			// Create parent directory
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating parent directory: %v\n", err)
				os.Exit(1)
			}
			
			// Create file
			outFile, err := os.OpenFile(target, os.O_CREATE|os.O_RDWR|os.O_TRUNC, os.FileMode(header.Mode))
			if err != nil {
				fmt.Fprintf(os.Stderr, "Error creating file %s: %v\n", target, err)
				os.Exit(1)
			}
			
			if _, err := io.Copy(outFile, tr); err != nil {
				outFile.Close()
				fmt.Fprintf(os.Stderr, "Error writing file %s: %v\n", target, err)
				os.Exit(1)
			}
			outFile.Close()
			
		case tar.TypeSymlink:
			// Create parent directory
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating parent directory: %v\n", err)
				os.Exit(1)
			}
			
			// Remove existing file/link if it exists
			os.Remove(target)
			
			// Create symlink
			if err := os.Symlink(header.Linkname, target); err != nil {
				fmt.Fprintf(os.Stderr, "Error creating symlink %s -> %s: %v\n", target, header.Linkname, err)
				os.Exit(1)
			}
		}
	}
	
	fmt.Println("Extraction complete to", destDir)
}
