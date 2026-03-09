package main

import (
	"archive/tar"
	"bufio"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"strings"
)

func main() {
	f, _ := os.Open("/tmp/APKINDEX.tar.gz")
	defer f.Close()
	
	gzr, _ := gzip.NewReader(f)
	defer gzr.Close()
	
	tr := tar.NewReader(gzr)
	
	for {
		header, err := tr.Next()
		if err == io.EOF {
			break
		}
		
		if strings.Contains(header.Name, "APKINDEX") {
			scanner := bufio.NewScanner(tr)
			var pkg, ver string
			
			for scanner.Scan() {
				line := scanner.Text()
				
				if strings.HasPrefix(line, "P:") {
					pkg = strings.TrimPrefix(line, "P:")
				} else if strings.HasPrefix(line, "V:") {
					ver = strings.TrimPrefix(line, "V:")
				} else if line == "" && pkg != "" {
					if strings.Contains(strings.ToLower(pkg), "icu") {
						fmt.Printf("%s %s\n", pkg, ver)
					}
					pkg = ""
					ver = ""
				}
			}
			break
		}
	}
}
