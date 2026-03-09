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
			
		case tar.TypeSymlink:
			if err := os.MkdirAll(filepath.Dir(target), 0755); err != nil {
				return err
			}
			
			os.Remove(target)
			
			if err := os.Symlink(header.Linkname, target); err != nil {
				return err
			}
		}
	}
	
	return nil
}

func main() {
	destDir := os.Getenv("HOME") + "/cpplibs"
	
	fmt.Println("Extracting icu-libs...")
	if err := extractApk("/tmp/icu-libs-arm64.apk", destDir); err != nil {
		fmt.Fprintf(os.Stderr, "Error extracting icu-libs: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println("Extracting icu-data-full...")
	if err := extractApk("/tmp/icu-data-full-arm64.apk", destDir); err != nil {
		fmt.Fprintf(os.Stderr, "Error extracting icu-data-full: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println("ICU extraction complete!")
}
