package main

import (
	"archive/tar"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"path/filepath"
)

func extractApk(apkFile, destDir string) error {
	// APK files are tar.gz files
	f, err := os.Open(apkFile)
	if err != nil {
		return err
	}
	defer f.Close()
	
	gzr, err := gzip.NewReader(f)
	if err != nil {
		return err
	}
	defer gzr.Close()
	
	tr := tar.NewReader(gzr)
	
	for {
		header, err := tr.Next()
		if err == io.EOF {
			break
		}
		if err != nil {
			return err
		}
		
		// Only extract files from /usr/lib/
		if !filepath.HasPrefix(header.Name, "usr/lib/") {
			continue
		}
		
		target := filepath.Join(destDir, header.Name)
		
		switch header.Typeflag {
		case tar.TypeDir:
			if err := os.MkdirAll(target, os.FileMode(header.Mode)); err != nil {
				return err
			}
		case tar.TypeReg:
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				return err
			}
			
			outFile, err := os.OpenFile(target, os.O_CREATE|os.O_RDWR|os.O_TRUNC, os.FileMode(header.Mode))
			if err != nil {
				return err
			}
			
			if _, err := io.Copy(outFile, tr); err != nil {
				outFile.Close()
				return err
			}
			outFile.Close()
			fmt.Printf("Extracted: %s\n", target)
			
		case tar.TypeSymlink:
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				return err
			}
			
			os.Remove(target)
			
			if err := os.Symlink(header.Linkname, target); err != nil {
				return err
			}
			fmt.Printf("Linked: %s -> %s\n", target, header.Linkname)
		}
	}
	
	return nil
}

func main() {
	destDir := os.Getenv("HOME") + "/cpplibs"
	
	os.RemoveAll(destDir)
	os.MkdirAll(destDir, 0755)
	
	fmt.Println("Extracting libgcc...")
	if err := extractApk("/tmp/libgcc-arm64.apk", destDir); err != nil {
		fmt.Fprintf(os.Stderr, "Error extracting libgcc: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println("Extracting libstdc++...")
	if err := extractApk("/tmp/libstdc++-arm64.apk", destDir); err != nil {
		fmt.Fprintf(os.Stderr, "Error extracting libstdc++: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println("Extraction complete!")
	fmt.Println("Libraries available in:", destDir)
}
