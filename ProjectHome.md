# Generating Vector Graphics for SBML into LaTeX #
SBML2TikZ provides automatic generation of PGF/TikZ TeX Macros to illustrate SBML graphs. The rendering is dependent on the SBML Render Extension proposed by Gauges et al. and the rendering library is built on the existing SBML Layout Library.

![https://sbml2tikz.googlecode.com/svn/trunk/wiki/Images/Color.jpg](https://sbml2tikz.googlecode.com/svn/trunk/wiki/Images/Color.jpg)

The TeX script produced by SBML2TikZ can be easily incorporated into LaTeX documents and converted into embedded images with the pdfLaTeX driver. Due to syntactic differences of PGF/TikZ macros in ConTeXT, the latter is currently not supported although this may change with future releases.

A collection of example TeX scripts and their compiled images can be seen in the Wiki, and compiled binaries for the latest stable release are available for download. SBML2TikZ is a work in progress, but please feel free to leave feedback!