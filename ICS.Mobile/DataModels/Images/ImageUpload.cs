using SqlPlusBase;
using System.ComponentModel.DataAnnotations;
using System.Runtime;

namespace ICS.Portal.Data.Images;

public partial class ImageInsertInput : ValidInput
{
    [Required]
    public int? WorkflowResultId { set; get; } = 0;

    [Required]
    public int? WorkflowStepResultId { set; get; } = 0;

    [Required]
    public int? WorkflowStepLoopIndex { set; get; } = 0;

    [Required]
    public int? ImageIndex { set; get; } = 0;

    [Required]
    public byte[]? Data { set; get; }

    [Required]
    public string? ContentType { set; get; }

    public Guid? TemporaryEquipmentId { set; get; }

    public int? EquipmentId { set; get; }

    public int? WorkflowId { set; get; }
    public int? WorkflowStepResultValueId { get; set; } = 0;
    public Guid? WorkflowFileId { set; get; }

    public Guid? NewsItemId { set; get; }

    public string? OriginalFileName { set; get; }
    
    private string? _generatedFileName = null;

    public string GeneratedFileName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_generatedFileName)) 
            {
                return _generatedFileName;
            }
            return $"{GeneratedFileNameWithoutExtension}.{GetExtensionFromMimeType(ContentType)}";
        }
        set
        {
            _generatedFileName = value;
        }
    }
    public string GeneratedFileNameWithoutExtension
    {
        get
        {
            if (WorkflowFileId.HasValue)
            {
                return $"wf-{WorkflowId}-{WorkflowFileId}";
            }
            if (TemporaryEquipmentId.HasValue)
            {
                return $"equip-{TemporaryEquipmentId}-{ImageIndex}";
            }
            if (EquipmentId.HasValue)
            {
                return $"equip-{EquipmentId}-{ImageIndex}";
            }
            if (NewsItemId.HasValue)
            {
                return $"news-{NewsItemId}";
            }
            return $"{WorkflowResultId}-{WorkflowStepResultId}-{WorkflowStepLoopIndex}-{ImageIndex}";
        }
       
    }

    public long Id
    {
        get
        {
            if (TemporaryEquipmentId != null)
            {
                char[] rawChars = TemporaryEquipmentId!.ToString()!.ToCharArray();
                char[] numberChars = new char[] { '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', };
                int idx = 0;
                foreach (var c in rawChars)
                {
                    if (idx == 10)
                    {
                        break;
                    }
                    if (char.IsDigit(c))
                    {
                        numberChars[idx++] = c;
                    }
                }
                string temporaryEquipmentId = $"{new string(numberChars).PadRight(11, '0')}{ImageIndex!.Value.ToString().PadRight(2, '0')}";
                return long.Parse(temporaryEquipmentId);
            }

            if (EquipmentId.HasValue && EquipmentId.Value != 0)
            {
                return long.Parse($"100000000{ImageIndex}{EquipmentId}");
            }

            string id = $"{WorkflowStepResultId!.Value.ToString().PadRight(11, '0')}{WorkflowStepLoopIndex!.Value.ToString().PadRight(3, '0')}{ImageIndex!.Value.ToString().PadRight(2, '0')}";
            return long.Parse(id);
        }
    }

    public Uri GetURL(string baseUrl=null)
    {
        if (!string.IsNullOrEmpty(baseUrl ))
            return new Uri($"{baseUrl}/{GeneratedFileName}");
        else
            return new Uri($"https://interiorcsstorage.blob.core.windows.net/images/{GeneratedFileName}");
        
    }

    
    public string Base64Version(string? ContentTypeOverride = null)
    {
        string contentType = ContentTypeOverride ?? ContentType ?? "application/octet-stream";
        if (Data is not null && Data.Length != 0)
        {
            return $"data:{contentType};base64,{Convert.ToBase64String(Data)}";
        }
        return string.Empty;
    }



    public bool IsVideo()
    {
        if (string.IsNullOrEmpty(ContentType)) 
            return false;

        return ContentType.StartsWith("video");
    }

    public bool IsImage()
    {
        if (string.IsNullOrEmpty(ContentType))
            return false;

        return ContentType.StartsWith("image");
    }
    public bool IsOtherFileType()
    {
        if (!IsImage() && !IsVideo())
            return true;
        else
            return false;
    }

    public void ParseContentType(string fileName)
    {
        int idx = fileName.LastIndexOf('.');
        string extension = fileName.Substring(idx + 1);
        ContentType = GetMimeTypeFromExtension(extension);
    }

    public bool Required { set; get; }

    public string? ImageLabel { set; get; }

    public string RequiredLabel
    {
        get
        {
            if (Required)
            {
                return "(Required)";
            }
            return "(Optional)";
        }
    }

    public static string GetMimeTypeFromExtension(string extension)
    {
        switch (extension.ToLower())
        {
            case "png": return "image/png";
            case "apng": return "image/apng";

            case "jpe": return "image/jpeg";
            case "jpeg": return "image/jpeg";
            case "jpg": return "image/jpeg";
            case "jpgv": return "video/jpeg";

            case "gif": return "image/gif";

            case "svg": return "image/svg+xml";
            case "svgz": return "image/svg+xml";

            case "pdf": return "application/pdf";

            case "weba": return "audio/webm";
            case "webm": return "video/webm";
            case "webp": return "image/webp";


            case "123": return "application/vnd.lotus-1-2-3";
            case "3dml": return "text/vnd.in3d.3dml";
            case "3ds": return "image/x-3ds";
            case "3g2": return "video/3gpp2";
            case "3gp": return "video/3gpp";
            case "7z": return "application/x-7z-compressed";
            case "aab": return "application/x-authorware-bin";
            case "aac": return "audio/x-aac";
            case "aam": return "application/x-authorware-map";
            case "aas": return "application/x-authorware-seg";
            case "abw": return "application/x-abiword";
            case "ac": return "application/pkix-attr-cert";
            case "acc": return "application/vnd.americandynamics.acc";
            case "ace": return "application/x-ace-compressed";
            case "acu": return "application/vnd.acucobol";
            case "adp": return "audio/adpcm";
            case "aep": return "application/vnd.audiograph";
            case "afp": return "application/vnd.ibm.modcap";
            case "ahead": return "application/vnd.ahead.space";
            case "ai": return "application/postscript";
            case "aif": return "audio/x-aiff";
            case "air": return "application/vnd.adobe.air-application-installer-package+zip";
            case "ait": return "application/vnd.dvb.ait";
            case "ami": return "application/vnd.amiga.ami";
            case "apk": return "application/vnd.android.package-archive";

            case "appcache": return "text/cache-manifest";
            case "apr": return "application/vnd.lotus-approach";
            case "arc": return "application/x-freearc";
            case "asc": return "application/pgp-signature";
            case "asf": return "video/x-ms-asf";
            case "asm": return "text/x-asm";
            case "aso": return "application/vnd.accpac.simply.aso";
            case "asx": return "video/x-ms-asf";
            case "atc": return "application/vnd.acucorp";
            case "atom": return "application/atom+xml";
            case "atomcat": return "application/atomcat+xml";
            case "atomsvc": return "application/atomsvc+xml";
            case "atx": return "application/vnd.antix.game-component";
            case "au": return "audio/basic";
            case "avi": return "video/x-msvideo";
            case "aw": return "application/applixware";
            case "azf": return "application/vnd.airzip.filesecure.azf";
            case "azs": return "application/vnd.airzip.filesecure.azs";
            case "azw": return "application/vnd.amazon.ebook";
            case "bat": return "application/x-msdownload";
            case "bcpio": return "application/x-bcpio";
            case "bdf": return "application/x-font-bdf";
            case "bdm": return "application/vnd.syncml.dm+wbxml";
            case "bed": return "application/vnd.realvnc.bed";
            case "bh2": return "application/vnd.fujitsu.oasysprs";
            case "bin": return "application/octet-stream";
            case "blb": return "application/x-blorb";
            case "blorb": return "application/x-blorb";
            case "bmi": return "application/vnd.bmi";
            case "bmp": return "image/bmp";
            case "book": return "application/vnd.framemaker";
            case "box": return "application/vnd.previewsystems.box";
            case "boz": return "application/x-bzip2";
            case "bpk": return "application/octet-stream";
            case "btif": return "image/prs.btif";
            case "bz": return "application/x-bzip";
            case "bz2": return "application/x-bzip2";
            case "c": return "text/x-c";
            case "c11amc": return "application/vnd.cluetrust.cartomobile-config";
            case "c11amz": return "application/vnd.cluetrust.cartomobile-config-pkg";
            case "c4g": return "application/vnd.clonk.c4group";
            case "cab": return "application/vnd.ms-cab-compressed";
            case "caf": return "audio/x-caf";
            case "car": return "application/vnd.curl.car";
            case "cat": return "application/vnd.ms-pki.seccat";
            case "cb7": return "application/x-cbr";
            case "cba": return "application/x-cbr";
            case "cbr": return "application/x-cbr";
            case "cbt": return "application/x-cbr";
            case "cbz": return "application/x-cbr";
            case "cco": return "application/x-cocoa";
            case "cct": return "application/x-director";
            case "ccxml": return "application/ccxml+xml";
            case "cdbcmsg": return "application/vnd.contact.cmsg";
            case "cdkey": return "application/vnd.mediastation.cdkey";
            case "cdmia": return "application/cdmi-capability";
            case "cdmic": return "application/cdmi-container";
            case "cdmid": return "application/cdmi-domain";
            case "cdmio": return "application/cdmi-object";
            case "cdmiq": return "application/cdmi-queue";
            case "cdx": return "chemical/x-cdx";
            case "cdxml": return "application/vnd.chemdraw+xml";
            case "cdy": return "application/vnd.cinderella";
            case "cer": return "application/pkix-cert";
            case "cfs": return "application/x-cfs-compressed";
            case "cgm": return "image/cgm";
            case "chat": return "application/x-chat";
            case "chm": return "application/vnd.ms-htmlhelp";
            case "chrt": return "application/vnd.kde.kchart";
            case "cif": return "chemical/x-cif";
            case "cii": return "application/vnd.anser-web-certificate-issue-initiation";
            case "cil": return "application/vnd.ms-artgalry";
            case "cla": return "application/vnd.claymore";
            case "class": return "application/java-vm";
            case "clkk": return "application/vnd.crick.clicker.keyboard";
            case "clkp": return "application/vnd.crick.clicker.palette";
            case "clkt": return "application/vnd.crick.clicker.template";
            case "clkw": return "application/vnd.crick.clicker.wordbank";
            case "clkx": return "application/vnd.crick.clicker";
            case "clp": return "application/x-msclip";
            case "cmc": return "application/vnd.cosmocaller";
            case "cmdf": return "chemical/x-cmdf";
            case "cml": return "chemical/x-cml";
            case "cmp": return "application/vnd.yellowriver-custom-menu";
            case "cmx": return "image/x-cmx";
            case "cod": return "application/vnd.rim.cod";
            case "com": return "application/x-msdownload";
            case "conf": return "text/plain";
            case "cpio": return "application/x-cpio";
            case "cpt": return "application/mac-compactpro";
            case "crd": return "application/x-mscardfile";
            case "crl": return "application/pkix-crl";
            case "crt": return "application/x-x509-ca-cert";
            case "cryptonote": return "application/vnd.rig.cryptonote";
            case "csh": return "application/x-csh";
            case "csml": return "chemical/x-csml";
            case "csp": return "application/vnd.commonspace";
            case "css": return "text/css";
            case "cst": return "application/x-director";
            case "csv": return "text/csv";
            case "cu": return "application/cu-seeme";
            case "curl": return "text/vnd.curl";
            case "cww": return "application/prs.cww";
            case "cxt": return "application/x-director";
            case "cxx": return "text/x-c";
            case "dae": return "model/vnd.collada+xml";
            case "daf": return "application/vnd.mobius.daf";
            case "dart": return "application/vnd.dart";
            case "dataless": return "application/vnd.fdsn.seed";
            case "davmount": return "application/davmount+xml";
            case "dbk": return "application/docbook+xml";
            case "dcr": return "application/x-director";
            case "dcurl": return "text/vnd.curl.dcurl";
            case "dd2": return "application/vnd.oma.dd2+xml";
            case "ddd": return "application/vnd.fujixerox.ddd";
            case "deb": return "application/x-debian-package";
            case "def": return "text/plain";
            case "deploy": return "application/octet-stream";
            case "der": return "application/x-x509-ca-cert";
            case "dfac": return "application/vnd.dreamfactory";
            case "dgc": return "application/x-dgc-compressed";
            case "dic": return "text/x-c";
            case "dir": return "application/x-director";
            case "dis": return "application/vnd.mobius.dis";
            case "dist": return "application/octet-stream";
            case "distz": return "application/octet-stream";
            case "djv": return "image/vnd.djvu";
            case "djvu": return "image/vnd.djvu";
            case "dll": return "application/x-msdownload";
            case "dmg": return "application/x-apple-diskimage";
            case "dmp": return "application/vnd.tcpdump.pcap";
            case "dms": return "application/octet-stream";
            case "dna": return "application/vnd.dna";
            case "doc": return "application/msword";
            case "docm": return "application/vnd.ms-word.document.macroenabled.12";
            case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case "dot": return "application/msword";
            case "dotm": return "application/vnd.ms-word.template.macroenabled.12";
            case "dotx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
            case "dp": return "application/vnd.osgi.dp";
            case "dpg": return "application/vnd.dpgraph";
            case "dra": return "audio/vnd.dra";
            case "dsc": return "text/prs.lines.tag";
            case "dssc": return "application/dssc+der";
            case "dtb": return "application/x-dtbook+xml";
            case "dtd": return "application/xml-dtd";
            case "dts": return "audio/vnd.dts";
            case "dtshd": return "audio/vnd.dts.hd";
            case "dump": return "application/octet-stream";
            case "dvb": return "video/vnd.dvb.file";
            case "dvi": return "application/x-dvi";
            case "dwf": return "model/vnd.dwf";
            case "dwg": return "image/vnd.dwg";
            case "dxf": return "image/vnd.dxf";
            case "dxp": return "application/vnd.spotfire.dxp";
            case "dxr": return "application/x-director";
            case "ecelp4800": return "audio/vnd.nuera.ecelp4800";
            case "ecelp7470": return "audio/vnd.nuera.ecelp7470";
            case "ecelp9600": return "audio/vnd.nuera.ecelp9600";
            case "ecma": return "application/ecmascript";
            case "edm": return "application/vnd.novadigm.edm";
            case "edx": return "application/vnd.novadigm.edx";
            case "efif": return "application/vnd.picsel";
            case "ei6": return "application/vnd.pg.osasli";
            case "elc": return "application/octet-stream";
            case "emf": return "application/x-msmetafile";
            case "eml": return "message/rfc822";
            case "emma": return "application/emma+xml";
            case "emz": return "application/x-msmetafile";
            case "eol": return "audio/vnd.digital-winds";
            case "eot": return "application/vnd.ms-fontobject";
            case "eps": return "application/postscript";
            case "epub": return "application/epub+zip";
            case "es": return "application/ecmascript";
            case "es3": return "application/vnd.eszigno3+xml";
            case "esa": return "application/vnd.osgi.subsystem";
            case "esf": return "application/vnd.epson.esf";
            case "et3": return "application/vnd.eszigno3+xml";
            case "etx": return "text/x-setext";
            case "eva": return "application/x-eva";
            case "evy": return "application/x-envoy";
            case "exe": return "application/x-msdownload";
            case "exi": return "application/exi";
            case "ext": return "application/vnd.novadigm.ext";
            case "ez": return "application/andrew-inset";
            case "ez2": return "application/vnd.ezpix-album";
            case "ez3": return "application/vnd.ezpix-package";
            case "f": return "text/x-fortran";
            case "f4v": return "video/x-f4v";
            case "f77": return "text/x-fortran";
            case "f90": return "text/x-fortran";
            case "fbs": return "image/vnd.fastbidsheet";
            case "fcdt": return "application/vnd.adobe.formscentral.fcdt";
            case "fcs": return "application/vnd.isac.fcs";
            case "fdf": return "application/vnd.fdf";
            case "fe_launch": return "application/vnd.denovo.fcselayout-link";
            case "fg5": return "application/vnd.fujitsu.oasysgp";
            case "fgd": return "application/x-director";
            case "fh": return "image/x-freehand";
            case "fh4": return "image/x-freehand";
            case "fh5": return "image/x-freehand";
            case "fh7": return "image/x-freehand";
            case "fhc": return "image/x-freehand";
            case "fig": return "application/x-xfig";
            case "flac": return "audio/x-flac";
            case "fli": return "video/x-fli";
            case "flo": return "application/vnd.micrografx.flo";
            case "flv": return "video/x-flv";
            case "flw": return "application/vnd.kde.kivio";
            case "flx": return "text/vnd.fmi.flexstor";
            case "fly": return "text/vnd.fly";
            case "fm": return "application/vnd.framemaker";
            case "fnc": return "application/vnd.frogans.fnc";
            case "for": return "text/x-fortran";
            case "fpx": return "image/vnd.fpx";
            case "frame": return "application/vnd.framemaker";
            case "fsc": return "application/vnd.fsc.weblaunch";
            case "fst": return "image/vnd.fst";
            case "ftc": return "application/vnd.fluxtime.clip";
            case "fti": return "application/vnd.anser-web-funds-transfer-initiation";
            case "fvt": return "video/vnd.fvt";
            case "fxp": return "application/vnd.adobe.fxp";
            case "fxpl": return "application/vnd.adobe.fxp";
            case "fzs": return "application/vnd.fuzzysheet";
            case "g2w": return "application/vnd.geoplan";
            case "g3": return "image/g3fax";
            case "g3w": return "application/vnd.geospace";
            case "gac": return "application/vnd.groove-account";
            case "gam": return "application/x-tads";
            case "gbr": return "application/rpki-ghostbusters";
            case "gca": return "application/x-gca-compressed";
            case "gdl": return "model/vnd.gdl";
            case "geo": return "application/vnd.dynageo";
            case "gex": return "application/vnd.geometry-explorer";
            case "ggb": return "application/vnd.geogebra.file";
            case "ggt": return "application/vnd.geogebra.tool";
            case "ghf": return "application/vnd.groove-help";

            case "gim": return "application/vnd.groove-identity-message";
            case "gml": return "application/gml+xml";
            case "gmx": return "application/vnd.gmx";
            case "gnumeric": return "application/x-gnumeric";
            case "gph": return "application/vnd.flographit";
            case "gpx": return "application/gpx+xml";
            case "gqf": return "application/vnd.grafeq";
            case "gqs": return "application/vnd.grafeq";
            case "gram": return "application/srgs";
            case "gramps": return "application/x-gramps-xml";
            case "gre": return "application/vnd.geometry-explorer";
            case "grv": return "application/vnd.groove-injector";
            case "grxml": return "application/srgs+xml";
            case "gsf": return "application/x-font-ghostscript";
            case "gtar": return "application/x-gtar";
            case "gtm": return "application/vnd.groove-tool-message";
            case "gtw": return "model/vnd.gtw";
            case "gv": return "text/vnd.graphviz";
            case "gxf": return "application/gxf";
            case "gxt": return "application/vnd.geonext";
            case "h": return "text/x-c";
            case "h261": return "video/h261";
            case "h263": return "video/h263";
            case "h264": return "video/h264";
            case "hal": return "application/vnd.hal+xml";
            case "hbci": return "application/vnd.hbci";
            case "hdf": return "application/x-hdf";
            case "hh": return "text/x-c";
            case "hlp": return "application/winhlp";
            case "hpgl": return "application/vnd.hp-hpgl";
            case "hpid": return "application/vnd.hp-hpid";
            case "hps": return "application/vnd.hp-hps";
            case "hqx": return "application/mac-binhex40";
            case "htke": return "application/vnd.kenameaapp";
            case "htm": return "text/html";
            case "html": return "text/html";
            case "hvd": return "application/vnd.yamaha.hv-dic";
            case "hvp": return "application/vnd.yamaha.hv-voice";
            case "hvs": return "application/vnd.yamaha.hv-script";
            case "i2g": return "application/vnd.intergeo";
            case "icc": return "application/vnd.iccprofile";
            case "ice": return "x-conference/x-cooltalk";
            case "icm": return "application/vnd.iccprofile";
            case "ico": return "image/x-icon";
            case "ics": return "text/calendar";
            case "ief": return "image/ief";
            case "ifb": return "text/calendar";
            case "ifm": return "application/vnd.shana.informed.formdata";
            case "iges": return "model/iges";
            case "igl": return "application/vnd.igloader";
            case "igm": return "application/vnd.insors.igm";
            case "igs": return "model/iges";
            case "igx": return "application/vnd.micrografx.igx";
            case "iif": return "application/vnd.shana.informed.interchange";
            case "imp": return "application/vnd.accpac.simply.imp";
            case "ims": return "application/vnd.ms-ims";
            case "in": return "text/plain";
            case "ink": return "application/inkml+xml";
            case "inkml": return "application/inkml+xml";
            case "install": return "application/x-install-instructions";
            case "iota": return "application/vnd.astraea-software.iota";
            case "ipfix": return "application/ipfix";
            case "ipk": return "application/vnd.shana.informed.package";
            case "irm": return "application/vnd.ibm.rights-management";
            case "irp": return "application/vnd.irepository.package+xml";
            case "iso": return "application/octet-stream";
            case "itp": return "application/vnd.shana.informed.formtemplate";
            case "ivp": return "application/vnd.immervision-ivp";
            case "ivu": return "application/vnd.immervision-ivu";
            case "jad": return "text/vnd.sun.j2me.app-descriptor";
            case "jam": return "application/vnd.jam";
            case "jar": return "application/java-archive";
            case "java": return "text/x-java-source";
            case "jisp": return "application/vnd.jisp";
            case "jlt": return "application/vnd.hp-jlyt";
            case "jnlp": return "application/x-java-jnlp-file";
            case "joda": return "application/vnd.joost.joda-archive";


            case "jpgm": return "video/jpm";
            case "jpm": return "video/jpm";
            case "js": return "application/javascript";
            case "json": return "application/json";
            case "jsonml": return "application/jsonml+json";
            case "kar": return "audio/midi";
            case "karbon": return "application/vnd.kde.karbon";
            case "kfo": return "application/vnd.kde.kformula";
            case "kia": return "application/vnd.kidspiration";
            case "kml": return "application/vnd.google-earth.kml+xml";
            case "kmz": return "application/vnd.google-earth.kmz";
            case "kne": return "application/vnd.kinar";
            case "knp": return "application/vnd.kinar";
            case "kon": return "application/vnd.kde.kontour";
            case "kpr": return "application/vnd.kde.kpresenter";
            case "kpt": return "application/vnd.kde.kpresenter";
            case "kpxx": return "application/vnd.ds-keypoint";
            case "ksp": return "application/vnd.kde.kspread";
            case "ktr": return "application/vnd.kahootz";
            case "ktx": return "image/ktx";
            case "ktz": return "application/vnd.kahootz";
            case "kwd": return "application/vnd.kde.kword";
            case "kwt": return "application/vnd.kde.kword";
            case "lasxml": return "application/vnd.las.las+xml";
            case "latex": return "application/x-latex";
            case "lbd": return "application/vnd.llamagraphics.life-balance.desktop";
            case "lbe": return "application/vnd.llamagraphics.life-balance.exchange+xml";
            case "les": return "application/vnd.hhe.lesson-player";
            case "lha": return "application/octet-stream";
            case "link66": return "application/vnd.route66.link66+xml";
            case "list": return "text/plain";
            case "list3820": return "application/vnd.ibm.modcap";
            case "listafp": return "application/vnd.ibm.modcap";
            case "lnk": return "application/x-ms-shortcut";
            case "log": return "text/plain";
            case "lostxml": return "application/lost+xml";
            case "lrf": return "application/octet-stream";
            case "lrm": return "application/vnd.ms-lrm";
            case "ltf": return "application/vnd.frogans.ltf";
            case "lvp": return "audio/vnd.lucent.voice";
            case "lwp": return "application/vnd.lotus-wordpro";
            case "lzh": return "application/octet-stream";
            case "m13": return "application/x-msmediaview";
            case "m14": return "application/x-msmediaview";
            case "m1v": return "video/mpeg";
            case "m21": return "application/mp21";
            case "m2a": return "audio/mpeg";
            case "m2v": return "video/mpeg";
            case "m3a": return "audio/mpeg";
            case "m3u": return "audio/x-mpegurl";
            case "m3u8": return "application/vnd.apple.mpegurl";
            case "m4a": return "audio/mp4";
            case "m4p": return "application/mp4";
            case "m4u": return "video/vnd.mpegurl";
            case "m4v": return "video/x-m4v";
            case "ma": return "application/mathematica";
            case "mads": return "application/mads+xml";
            case "mag": return "application/vnd.ecowin.chart";
            case "man": return "text/troff";
            case "mar": return "application/octet-stream";
            case "mathml": return "application/mathml+xml";
            case "mb": return "application/mathematica";
            case "mbk": return "application/vnd.mobius.mbk";
            case "mbox": return "application/mbox";
            case "mc1": return "application/vnd.medcalcdata";
            case "mcd": return "application/vnd.mcd";
            case "mcurl": return "text/vnd.curl.mcurl";
            case "mdb": return "application/x-msaccess";
            case "mdi": return "image/vnd.ms-modi";
            case "me": return "text/troff";
            case "mesh": return "model/mesh";
            case "meta4": return "application/metalink4+xml";
            case "mets": return "application/mets+xml";
            case "mfm": return "application/vnd.mfmp";
            case "mft": return "application/rpki-manifest";
            case "mgp": return "application/vnd.osgeo.mapguide.package";
            case "mgz": return "application/vnd.proteus.magazine";
            case "mid": return "audio/midi";
            case "midi": return "audio/midi";
            case "mie": return "application/x-mie";
            case "mif": return "application/vnd.mif";
            case "mime": return "message/rfc822";
            case "mj2": return "video/mj2";
            case "mjp2": return "video/mj2";
            case "mk3d": return "video/x-matroska";
            case "mka": return "audio/x-matroska";
            case "mks": return "video/x-matroska";
            case "mkv": return "video/x-matroska";
            case "mlp": return "application/vnd.dolby.mlp";
            case "mmd": return "application/vnd.chipnuts.karaoke-mmd";
            case "mmf": return "application/vnd.smaf";
            case "mmr": return "image/vnd.fujixerox.edmics-mmr";
            case "mng": return "video/x-mng";
            case "mny": return "application/x-msmoney";
            case "mobi": return "application/x-mobipocket-ebook";
            case "mods": return "application/mods+xml";
            case "mov": return "video/quicktime";
            case "movie": return "video/x-sgi-movie";
            case "mp2": return "audio/mpeg";
            case "mp21": return "application/mp21";
            case "mp2a": return "audio/mpeg";
            case "mp3": return "audio/mpeg";
            case "mp4": return "video/mp4";
            case "mp4a": return "audio/mp4";
            case "mp4s": return "application/mp4";
            case "mp4v": return "video/mp4";
            case "mpc": return "application/vnd.mophun.certificate";
            case "mpe": return "video/mpeg";
            case "mpeg": return "video/mpeg";
            case "mpg": return "video/mpeg";
            case "mpg4": return "video/mp4";
            case "mpga": return "audio/mpeg";
            case "mpkg": return "application/vnd.apple.installer+xml";
            case "mpm": return "application/vnd.blueice.multipass";
            case "mpn": return "application/vnd.mophun.application";
            case "mpp": return "application/vnd.ms-project";
            case "mpt": return "application/vnd.ms-project";
            case "mpy": return "application/vnd.ibm.minipay";
            case "mqy": return "application/vnd.mobius.mqy";
            case "mrc": return "application/marc";
            case "mrcx": return "application/marcxml+xml";
            case "ms": return "text/troff";
            case "mscml": return "application/mediaservercontrol+xml";
            case "mseed": return "application/vnd.fdsn.mseed";
            case "mseq": return "application/vnd.mseq";
            case "msf": return "application/vnd.epson.msf";
            case "msh": return "model/mesh";
            case "msi": return "application/x-msdownload";
            case "msl": return "application/vnd.mobius.msl";
            case "msty": return "application/vnd.muvee.style";
            case "mts": return "model/vnd.mts";
            case "mus": return "application/vnd.musician";
            case "musicxml": return "application/vnd.recordare.musicxml+xml";
            case "mvb": return "application/x-msmediaview";
            case "mwf": return "application/vnd.mfer";
            case "mxf": return "application/mxf";
            case "mxl": return "application/vnd.recordare.musicxml";
            case "mxml": return "application/xv+xml";
            case "mxs": return "application/vnd.triscape.mxs";
            case "mxu": return "video/vnd.mpegurl";
            case "n-gage": return "application/vnd.nokia.n-gage.symbian.install";
            case "n3": return "text/n3";
            case "nb": return "application/mathematica";
            case "nbp": return "application/vnd.wolfram.player";
            case "nc": return "application/x-netcdf";
            case "ncx": return "application/x-dtbncx+xml";
            case "nfo": return "text/x-nfo";
            case "ngdat": return "application/vnd.nokia.n-gage.data";
            case "nitf": return "application/vnd.nitf";
            case "nlu": return "application/vnd.neurolanguage.nlu";
            case "nml": return "application/vnd.enliven";
            case "nnd": return "application/vnd.noblenet-directory";
            case "nns": return "application/vnd.noblenet-sealer";
            case "nnw": return "application/vnd.noblenet-web";
            case "npx": return "image/vnd.net-fpx";
            case "nsc": return "application/x-conference";
            case "nsf": return "application/vnd.lotus-notes";
            case "ntf": return "application/vnd.nitf";
            case "nzb": return "application/x-nzb";
            case "oa2": return "application/vnd.fujitsu.oasys2";
            case "oa3": return "application/vnd.fujitsu.oasys3";
            case "oas": return "application/vnd.fujitsu.oasys";
            case "obd": return "application/x-msbinder";
            case "obj": return "application/x-tgif";
            case "oda": return "application/oda";
            case "odb": return "application/vnd.oasis.opendocument.database";
            case "odc": return "application/vnd.oasis.opendocument.chart";
            case "odf": return "application/vnd.oasis.opendocument.formula";
            case "odft": return "application/vnd.oasis.opendocument.formula-template";
            case "odg": return "application/vnd.oasis.opendocument.graphics";
            case "odi": return "application/vnd.oasis.opendocument.image";
            case "odm": return "application/vnd.oasis.opendocument.text-master";
            case "odp": return "application/vnd.oasis.opendocument.presentation";
            case "ods": return "application/vnd.oasis.opendocument.spreadsheet";
            case "odt": return "application/vnd.oasis.opendocument.text";
            case "oga": return "audio/ogg";
            case "ogg": return "audio/ogg";
            case "ogv": return "video/ogg";
            case "ogx": return "application/ogg";
            case "omdoc": return "application/omdoc+xml";
            case "onepkg": return "application/onenote";
            case "onetmp": return "application/onenote";
            case "onetoc": return "application/onenote";
            case "onetoc2": return "application/onenote";
            case "opf": return "application/oebps-package+xml";
            case "opml": return "text/x-opml";
            case "oprc": return "application/vnd.palm";
            case "org": return "application/vnd.lotus-organizer";
            case "osf": return "application/vnd.yamaha.openscoreformat";
            case "osfpvg": return "application/vnd.yamaha.openscoreformat.osfpvg+xml";
            case "otc": return "application/vnd.oasis.opendocument.chart-template";
            case "otf": return "font/otf";
            case "otg": return "application/vnd.oasis.opendocument.graphics-template";
            case "oth": return "application/vnd.oasis.opendocument.text-web";
            case "oti": return "application/vnd.oasis.opendocument.image-template";
            case "otp": return "application/vnd.oasis.opendocument.presentation-template";
            case "ots": return "application/vnd.oasis.opendocument.spreadsheet-template";
            case "ott": return "application/vnd.oasis.opendocument.text-template";
            case "oxps": return "application/oxps";
            case "oxt": return "application/vnd.openofficeorg.extension";
            case "p": return "text/x-pascal";
            case "p10": return "application/pkcs10";
            case "p12": return "application/x-pkcs12";
            case "p7b": return "application/x-pkcs7-certificates";
            case "p7c": return "application/pkcs7-mime";
            case "p7m": return "application/pkcs7-mime";
            case "p7r": return "application/x-pkcs7-certreqresp";
            case "p7s": return "application/pkcs7-signature";
            case "p8": return "application/pkcs8";
            case "pas": return "text/x-pascal";
            case "paw": return "application/vnd.pawaafile";
            case "pbd": return "application/vnd.powerbuilder6";
            case "pbm": return "image/x-portable-bitmap";
            case "pcap": return "application/vnd.tcpdump.pcap";
            case "pcf": return "application/x-font-pcf";
            case "pcl": return "application/vnd.hp-pcl";
            case "pclxl": return "application/vnd.hp-pclxl";
            case "pct": return "image/x-pict";
            case "pcurl": return "application/vnd.curl.pcurl";
            case "pcx": return "image/x-pcx";
            case "pdb": return "application/vnd.palm";

            case "pfa": return "application/x-font-type1";
            case "pfb": return "application/x-font-type1";
            case "pfm": return "application/x-font-type1";
            case "pfr": return "application/font-tdpfr";
            case "pfx": return "application/x-pkcs12";
            case "pgm": return "image/x-portable-graymap";
            case "pgn": return "application/x-chess-pgn";
            case "pgp": return "application/pgp-encrypted";
            case "pic": return "image/x-pict";
            case "pkg": return "application/octet-stream";
            case "pki": return "application/pkixcmp";
            case "pkipath": return "application/pkix-pkipath";
            case "plb": return "application/vnd.3gpp.pic-bw-large";
            case "plc": return "application/vnd.mobius.plc";
            case "plf": return "application/vnd.pocketlearn";
            case "pls": return "application/pls+xml";
            case "pml": return "application/vnd.ctc-posml";

            case "pnm": return "image/x-portable-anymap";
            case "portpkg": return "application/vnd.macports.portpkg";
            case "pot": return "application/vnd.ms-powerpoint";
            case "potm": return "application/vnd.ms-powerpoint.template.macroenabled.12";
            case "potx": return "application/vnd.openxmlformats-officedocument.presentationml.template";
            case "ppam": return "application/vnd.ms-powerpoint.addin.macroenabled.12";
            case "ppd": return "application/vnd.cups-ppd";
            case "ppm": return "image/x-portable-pixmap";
            case "pps": return "application/vnd.ms-powerpoint";
            case "ppsm": return "application/vnd.ms-powerpoint.slideshow.macroenabled.12";
            case "ppsx": return "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
            case "ppt": return "application/vnd.ms-powerpoint";
            case "pptm": return "application/vnd.ms-powerpoint.presentation.macroenabled.12";
            case "pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            case "pqa": return "application/vnd.palm";
            case "prc": return "application/x-mobipocket-ebook";
            case "pre": return "application/vnd.lotus-freelance";
            case "prf": return "application/pics-rules";
            case "ps": return "application/postscript";
            case "psb": return "application/vnd.3gpp.pic-bw-small";
            case "psd": return "image/vnd.adobe.photoshop";
            case "psf": return "application/x-font-linux-psf";
            case "pskcxml": return "application/pskc+xml";
            case "ptid": return "application/vnd.pvi.ptid1";
            case "pub": return "application/x-mspublisher";
            case "pvb": return "application/vnd.3gpp.pic-bw-var";
            case "pwn": return "application/vnd.3m.post-it-notes";
            case "pya": return "audio/vnd.ms-playready.media.pya";
            case "pyv": return "video/vnd.ms-playready.media.pyv";
            case "qam": return "application/vnd.epson.quickanime";
            case "qbo": return "application/vnd.intu.qbo";
            case "qfx": return "application/vnd.intu.qfx";
            case "qps": return "application/vnd.publishare-delta-tree";
            case "qt": return "video/quicktime";
            case "qxd": return "application/vnd.quark.quarkxpress";
            case "qxt": return "application/vnd.quark.quarkxpress";
            case "ra": return "audio/x-pn-realaudio";
            case "ram": return "audio/x-pn-realaudio";
            case "rar": return "application/x-rar-compressed";
            case "ras": return "image/x-cmu-raster";
            case "rcprofile": return "application/vnd.ipunplugged.rcprofile";
            case "rdf": return "application/rdf+xml";
            case "rdz": return "application/vnd.data-vision.rdz";
            case "rep": return "application/vnd.businessobjects";
            case "res": return "application/x-dtbresource+xml";
            case "rgb": return "image/x-rgb";
            case "rif": return "application/reginfo+xml";
            case "rip": return "audio/vnd.rip";
            case "ris": return "application/x-research-info-systems";
            case "rl": return "application/resource-lists+xml";
            case "rlc": return "image/vnd.fujixerox.edmics-rlc";
            case "rld": return "application/resource-lists-diff+xml";
            case "rm": return "application/vnd.rn-realmedia";
            case "rmi": return "audio/midi";
            case "rmp": return "audio/x-pn-realaudio-plugin";
            case "rms": return "application/vnd.jcp.javame.midlet-rms";
            case "rmvb": return "application/vnd.rn-realmedia-vbr";
            case "rnc": return "application/relax-ng-compact-syntax";
            case "roa": return "application/rpki-roa";
            case "roff": return "text/troff";
            case "rp9": return "application/vnd.cloanto.rp9";
            case "rpss": return "application/vnd.nokia.radio-presets";
            case "rpst": return "application/vnd.nokia.radio-preset";
            case "rq": return "application/sparql-query";
            case "rs": return "application/rls-services+xml";
            case "rsd": return "application/rsd+xml";
            case "rss": return "application/rss+xml";
            case "rtf": return "application/rtf";
            case "rtx": return "text/richtext";
            case "s": return "text/x-asm";
            case "s3m": return "audio/s3m";
            case "saf": return "application/vnd.yamaha.smaf-audio";
            case "sbml": return "application/sbml+xml";
            case "sc": return "application/vnd.ibm.secure-container";
            case "scd": return "application/x-msschedule";
            case "scm": return "application/vnd.lotus-screencam";
            case "scq": return "application/scvp-cv-request";
            case "scs": return "application/scvp-cv-response";
            case "scurl": return "text/vnd.curl.scurl";
            case "sda": return "application/vnd.stardivision.draw";
            case "sdc": return "application/vnd.stardivision.calc";
            case "sdd": return "application/vnd.stardivision.impress";
            case "sdkd": return "application/vnd.solent.sdkm+xml";
            case "sdkm": return "application/vnd.solent.sdkm+xml";
            case "sdp": return "application/sdp";
            case "sdw": return "application/vnd.stardivision.writer";
            case "see": return "application/vnd.seemail";
            case "seed": return "application/vnd.fdsn.seed";
            case "sema": return "application/vnd.sema";
            case "semd": return "application/vnd.semd";
            case "semf": return "application/vnd.semf";
            case "ser": return "application/java-serialized-object";
            case "setpay": return "application/set-payment-initiation";
            case "setreg": return "application/set-registration-initiation";
            case "sfd-hdstx": return "application/vnd.hydrostatix.sof-data";
            case "sfs": return "application/vnd.spotfire.sfs";
            case "sfv": return "text/x-sfv";
            case "sgi": return "image/sgi";
            case "sgl": return "application/vnd.stardivision.writer-global";
            case "sgm": return "text/sgml";
            case "sgml": return "text/sgml";
            case "sh": return "application/x-sh";
            case "shar": return "application/x-shar";
            case "shf": return "application/shf+xml";
            case "sid": return "image/x-mrsid-image";
            case "sig": return "application/pgp-signature";
            case "sil": return "audio/silk";
            case "silo": return "model/mesh";
            case "sis": return "application/vnd.symbian.install";
            case "sisx": return "application/vnd.symbian.install";
            case "sit": return "application/x-stuffit";
            case "sitx": return "application/x-stuffitx";
            case "siv": return "application/sieve";
            case "skd": return "application/vnd.koan";
            case "skm": return "application/vnd.koan";
            case "skp": return "application/vnd.koan";
            case "skt": return "application/vnd.koan";
            case "sldm": return "application/vnd.ms-powerpoint.slide.macroenabled.12";
            case "sldx": return "application/vnd.openxmlformats-officedocument.presentationml.slide";
            case "slt": return "application/vnd.epson.salt";
            case "sm": return "application/vnd.stepmania.stepchart";
            case "smf": return "application/vnd.stardivision.math";
            case "smi": return "application/smil+xml";
            case "smil": return "application/smil+xml";
            case "smv": return "video/x-smv";
            case "smzip": return "application/vnd.stepmania.package";
            case "snf": return "application/x-font-snf";
            case "so": return "application/octet-stream";
            case "spc": return "application/x-pkcs7-certificates";
            case "spf": return "application/vnd.yamaha.smaf-phrase";
            case "spl": return "application/x-futuresplash";
            case "spot": return "text/vnd.in3d.spot";
            case "spp": return "application/scvp-vp-response";
            case "spq": return "application/scvp-vp-request";
            case "spx": return "audio/ogg";
            case "sql": return "application/x-sql";
            case "src": return "application/x-wais-source";
            case "srt": return "application/x-subrip";
            case "sru": return "application/sru+xml";
            case "srx": return "application/sparql-results+xml";
            case "ssdl": return "application/ssdl+xml";
            case "sse": return "application/vnd.kodak-descriptor";
            case "ssf": return "application/vnd.epson.ssf";
            case "ssml": return "application/ssml+xml";
            case "st": return "application/vnd.sailingtracker.track";
            case "stc": return "application/vnd.sun.xml.calc.template";
            case "std": return "application/vnd.sun.xml.draw.template";
            case "stf": return "application/vnd.wt.stf";
            case "sti": return "application/vnd.sun.xml.impress.template";
            case "stk": return "application/hyperstudio";
            case "stl": return "application/vnd.ms-pki.stl";
            case "str": return "application/vnd.pg.format";
            case "stw": return "application/vnd.sun.xml.writer.template";
            case "sub": return "image/vnd.dvb.subtitle";
            case "sus": return "application/vnd.sus-calendar";
            case "susp": return "application/vnd.sus-calendar";
            case "sv4cpio": return "application/x-sv4cpio";
            case "sv4crc": return "application/x-sv4crc";
            case "svc": return "application/vnd.dvb.service";
            case "svd": return "application/vnd.svd";

            case "swa": return "application/x-director";
            case "swf": return "application/x-shockwave-flash";
            case "swi": return "application/vnd.aristanetworks.swi";
            case "sxc": return "application/vnd.sun.xml.calc";
            case "sxd": return "application/vnd.sun.xml.draw";
            case "sxg": return "application/vnd.sun.xml.writer.global";
            case "sxi": return "application/vnd.sun.xml.impress";
            case "sxm": return "application/vnd.sun.xml.math";
            case "sxw": return "application/vnd.sun.xml.writer";
            case "t": return "text/troff";
            case "t3": return "application/x-t3vm-image";
            case "taglet": return "application/vnd.mynfc";
            case "tao": return "application/vnd.tao.intent-module-archive";
            case "tar": return "application/x-tar";
            case "tcap": return "application/vnd.3gpp2.tcap";
            case "tcl": return "application/x-tcl";
            case "teacher": return "application/vnd.smart.teacher";
            case "tei": return "application/tei+xml";
            case "teicorpus": return "application/tei+xml";
            case "tex": return "application/x-tex";
            case "texi": return "application/x-texinfo";
            case "texinfo": return "application/x-texinfo";
            case "text": return "text/plain";
            case "tfi": return "application/thraud+xml";
            case "tfm": return "application/x-tex-tfm";
            case "tga": return "image/x-tga";
            case "thmx": return "application/vnd.ms-officetheme";
            case "tif": return "image/tiff";
            case "tiff": return "image/tiff";
            case "tmo": return "application/vnd.tmobile-livetv";
            case "torrent": return "application/x-bittorrent";
            case "tpl": return "application/vnd.groove-tool-template";
            case "tpt": return "application/vnd.trid.tpt";
            case "tr": return "text/troff";
            case "tra": return "application/vnd.trueapp";
            case "trm": return "application/x-msterminal";
            case "tsd": return "application/timestamped-data";
            case "tsv": return "text/tab-separated-values";
            case "ttc": return "font/collection";
            case "ttf": return "font/ttf";
            case "ttl": return "text/turtle";
            case "twd": return "application/vnd.simtech-mindmapper";
            case "twds": return "application/vnd.simtech-mindmapper";
            case "txd": return "application/vnd.genomatix.tuxedo";
            case "txf": return "application/vnd.mobius.txf";
            case "txt": return "text/plain";
            case "u32": return "application/x-authorware-bin";
            case "udeb": return "application/x-debian-package";
            case "ufd": return "application/vnd.ufdl";
            case "ufdl": return "application/vnd.ufdl";
            case "ulx": return "application/x-glulx";
            case "umj": return "application/vnd.umajin";
            case "unityweb": return "application/vnd.unity";
            case "uoml": return "application/vnd.uoml+xml";
            case "uri": return "text/uri-list";
            case "uris": return "text/uri-list";
            case "urls": return "text/uri-list";
            case "ustar": return "application/x-ustar";
            case "utz": return "application/vnd.uiq.theme";
            case "uu": return "text/x-uuencode";
            case "uva": return "audio/vnd.dece.audio";
            case "uvd": return "application/vnd.dece.data";
            case "uvf": return "application/vnd.dece.data";
            case "uvg": return "image/vnd.dece.graphic";
            case "uvh": return "video/vnd.dece.hd";
            case "uvi": return "image/vnd.dece.graphic";
            case "uvm": return "video/vnd.dece.mobile";
            case "uvp": return "video/vnd.dece.pd";
            case "uvs": return "video/vnd.dece.sd";
            case "uvt": return "application/vnd.dece.ttml+xml";
            case "uvu": return "video/vnd.uvvu.mp4";
            case "uvv": return "video/vnd.dece.video";
            case "uvva": return "audio/vnd.dece.audio";
            case "uvvd": return "application/vnd.dece.data";
            case "uvvf": return "application/vnd.dece.data";
            case "uvvg": return "image/vnd.dece.graphic";
            case "uvvh": return "video/vnd.dece.hd";
            case "uvvi": return "image/vnd.dece.graphic";
            case "uvvm": return "video/vnd.dece.mobile";
            case "uvvp": return "video/vnd.dece.pd";
            case "uvvs": return "video/vnd.dece.sd";
            case "uvvt": return "application/vnd.dece.ttml+xml";
            case "uvvu": return "video/vnd.uvvu.mp4";
            case "uvvv": return "video/vnd.dece.video";
            case "uvvx": return "application/vnd.dece.unspecified";
            case "uvvz": return "application/vnd.dece.zip";
            case "uvx": return "application/vnd.dece.unspecified";
            case "uvz": return "application/vnd.dece.zip";
            case "vcard": return "text/vcard";
            case "vcd": return "application/x-cdlink";
            case "vcf": return "text/x-vcard";
            case "vcg": return "application/vnd.groove-vcard";
            case "vcs": return "text/x-vcalendar";
            case "vcx": return "application/vnd.vcx";
            case "vis": return "application/vnd.visionary";
            case "viv": return "video/vnd.vivo";
            case "vob": return "video/x-ms-vob";
            case "vor": return "application/vnd.stardivision.writer";
            case "vox": return "application/x-authorware-bin";
            case "vrml": return "model/vrml";
            case "vsd": return "application/vnd.visio";
            case "vsf": return "application/vnd.vsf";
            case "vss": return "application/vnd.visio";
            case "vst": return "application/vnd.visio";
            case "vsw": return "application/vnd.visio";
            case "vtu": return "model/vnd.vtu";
            case "vxml": return "application/voicexml+xml";
            case "w3d": return "application/x-director";
            case "wad": return "application/x-doom";
            case "wav": return "audio/x-wav";
            case "wax": return "audio/x-ms-wax";
            case "wbmp": return "image/vnd.wap.wbmp";
            case "wbs": return "application/vnd.criticaltools.wbs+xml";
            case "wbxml": return "application/vnd.wap.wbxml";
            case "wcm": return "application/vnd.ms-works";
            case "wdb": return "application/vnd.ms-works";
            case "wdp": return "image/vnd.ms-photo";
            case "wg": return "application/vnd.pmi.widget";
            case "wgt": return "application/widget";
            case "wks": return "application/vnd.ms-works";
            case "wm": return "video/x-ms-wm";
            case "wma": return "audio/x-ms-wma";
            case "wmd": return "application/x-ms-wmd";
            case "wmf": return "application/x-msmetafile";
            case "wml": return "text/vnd.wap.wml";
            case "wmlc": return "application/vnd.wap.wmlc";
            case "wmls": return "text/vnd.wap.wmlscript";
            case "wmlsc": return "application/vnd.wap.wmlscriptc";
            case "wmv": return "video/x-ms-wmv";
            case "wmx": return "video/x-ms-wmx";
            case "wmz": return "application/x-msmetafile";
            case "woff": return "font/woff";
            case "woff2": return "font/woff2";
            case "wpd": return "application/vnd.wordperfect";
            case "wpl": return "application/vnd.ms-wpl";
            case "wps": return "application/vnd.ms-works";
            case "wqd": return "application/vnd.wqd";
            case "wri": return "application/x-mswrite";
            case "wrl": return "model/vrml";
            case "wsdl": return "application/wsdl+xml";
            case "wspolicy": return "application/wspolicy+xml";
            case "wtb": return "application/vnd.webturbo";
            case "wvx": return "video/x-ms-wvx";
            case "x32": return "application/x-authorware-bin";
            case "x3d": return "model/x3d+xml";
            case "x3db": return "model/x3d+binary";
            case "x3dbz": return "model/x3d+binary";
            case "x3dv": return "model/x3d+vrml";
            case "x3dvz": return "model/x3d+vrml";
            case "x3dz": return "model/x3d+xml";
            case "xaml": return "application/xaml+xml";
            case "xap": return "application/x-silverlight-app";
            case "xar": return "application/vnd.xara";
            case "xbap": return "application/x-ms-xbap";
            case "xbd": return "application/vnd.fujixerox.docuworks.binder";
            case "xbm": return "image/x-xbitmap";
            case "xdf": return "application/xcap-diff+xml";
            case "xdm": return "application/vnd.syncml.dm+xml";
            case "xdp": return "application/vnd.adobe.xdp+xml";
            case "xdssc": return "application/dssc+xml";
            case "xdw": return "application/vnd.fujixerox.docuworks";
            case "xenc": return "application/xenc+xml";
            case "xer": return "application/patch-ops-error+xml";
            case "xfdf": return "application/vnd.adobe.xfdf";
            case "xfdl": return "application/vnd.xfdl";
            case "xht": return "application/xhtml+xml";
            case "xhtml": return "application/xhtml+xml";
            case "xhvml": return "application/xv+xml";
            case "xif": return "image/vnd.xiff";
            case "xla": return "application/vnd.ms-excel";
            case "xlam": return "application/vnd.ms-excel.addin.macroenabled.12";
            case "xlc": return "application/vnd.ms-excel";
            case "xlf": return "application/x-xliff+xml";
            case "xlm": return "application/vnd.ms-excel";
            case "xls": return "application/vnd.ms-excel";
            case "xlsb": return "application/vnd.ms-excel.sheet.binary.macroenabled.12";
            case "xlsm": return "application/vnd.ms-excel.sheet.macroenabled.12";
            case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case "xlt": return "application/vnd.ms-excel";
            case "xltm": return "application/vnd.ms-excel.template.macroenabled.12";
            case "xltx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
            case "xlw": return "application/vnd.ms-excel";
            case "xml": return "application/xml";
            case "xo": return "application/vnd.olpc-sugar";
            case "xop": return "application/xop+xml";
            case "xpi": return "application/x-xpinstall";
            case "xpl": return "application/xproc+xml";
            case "xpm": return "image/x-xpixmap";
            case "xpr": return "application/vnd.is-xpr";
            case "xps": return "application/vnd.ms-xpsdocument";
            case "xpw": return "application/vnd.intercon.formnet";
            case "xpx": return "application/vnd.intercon.formnet";
            case "xsl": return "application/xml";
            case "xslt": return "application/xslt+xml";
            case "xsm": return "application/vnd.syncml+xml";
            case "xspf": return "application/xspf+xml";
            case "xul": return "application/vnd.mozilla.xul+xml";
            case "xvm": return "application/xv+xml";
            case "xvml": return "application/xv+xml";
            case "xwd": return "image/x-xwindowdump";
            case "xyz": return "chemical/x-xyz";
            case "xz": return "application/x-xz";
            case "yaml": return "text/yaml";
            case "yang": return "application/yang";
            case "yin": return "application/yin+xml";
            case "zaz": return "application/vnd.zzazz.deck+xml";
            case "zip": return "application/zip";
            case "zir": return "application/vnd.zul";
            case "zirz": return "application/vnd.zul";
            case "zmm": return "application/vnd.handheld-entertainment+xml";
            case "zst": return "application/zstd";
            default: return "application/octet-stream";
        }


    }

    public static string GetExtensionFromMimeType(string mimeType)
    {
        switch (mimeType.ToLower())
        {
            case "image/jpeg": return "jpg";
            case "image/png": return "png";
            case "image/gif": return "gif";
            case "image/svg+xml": return "svg";
            case "application/pdf": return "pdf";

            case "application/vnd.lotus-1-2-3": return "123";
            case "text/vnd.in3d.3dml": return "3dml";
            case "image/x-3ds": return "3ds";
            case "video/3gpp2": return "3g2";
            case "video/3gpp": return "3gp";
            case "application/x-7z-compressed": return "7z";
            case "application/x-authorware-bin": return "aab";
            case "audio/x-aac": return "aac";
            case "application/x-authorware-map": return "aam";
            case "application/x-authorware-seg": return "aas";
            case "application/x-abiword": return "abw";
            case "application/pkix-attr-cert": return "ac";
            case "application/vnd.americandynamics.acc": return "acc";
            case "application/x-ace-compressed": return "ace";
            case "application/vnd.acucobol": return "acu";
            case "audio/adpcm": return "adp";
            case "application/vnd.audiograph": return "aep";
            case "application/vnd.ibm.modcap": return "afp";
            case "application/vnd.ahead.space": return "ahead";
            case "audio/x-aiff": return "aif";
            case "application/vnd.adobe.air-application-installer-package+zip": return "air";
            case "application/vnd.dvb.ait": return "ait";
            case "application/vnd.amiga.ami": return "ami";
            case "application/vnd.android.package-archive": return "apk";
            case "image/apng": return "apng";
            case "text/cache-manifest": return "appcache";
            case "application/vnd.lotus-approach": return "apr";
            case "application/x-freearc": return "arc";
            case "video/x-ms-asf": return "asf";
            case "text/x-asm": return "asm";
            case "application/vnd.accpac.simply.aso": return "aso";
            case "application/vnd.acucorp": return "atc";
            case "application/atom+xml": return "atom";
            case "application/atomcat+xml": return "atomcat";
            case "application/atomsvc+xml": return "atomsvc";
            case "application/vnd.antix.game-component": return "atx";
            case "audio/basic": return "au";
            case "video/x-msvideo": return "avi";
            case "application/applixware": return "aw";
            case "application/vnd.airzip.filesecure.azf": return "azf";
            case "application/vnd.airzip.filesecure.azs": return "azs";
            case "application/vnd.amazon.ebook": return "azw";
            case "application/x-bcpio": return "bcpio";
            case "application/x-font-bdf": return "bdf";
            case "application/vnd.syncml.dm+wbxml": return "bdm";
            case "application/vnd.realvnc.bed": return "bed";
            case "application/vnd.fujitsu.oasysprs": return "bh2";
            case "application/octet-stream": return "bin";
            case "application/x-blorb": return "blb";
            case "application/vnd.bmi": return "bmi";
            case "image/bmp": return "bmp";
            case "application/vnd.previewsystems.box": return "box";
            case "application/x-bzip2": return "bz2";
            case "application/x-bzip": return "bz";
            case "application/vnd.cluetrust.cartomobile-config": return "c11amc";
            case "application/vnd.cluetrust.cartomobile-config-pkg": return "c11amz";
            case "application/vnd.clonk.c4group": return "c4g";
            case "application/vnd.ms-cab-compressed": return "cab";
            case "audio/x-caf": return "caf";
            case "application/vnd.curl.car": return "car";
            case "application/vnd.ms-pki.seccat": return "cat";
            case "application/x-cbr": return "cbr";
            case "application/x-cocoa": return "cco";
            case "application/ccxml+xml": return "ccxml";
            case "application/vnd.contact.cmsg": return "cdbcmsg";
            case "application/vnd.mediastation.cdkey": return "cdkey";
            case "application/cdmi-capability": return "cdmia";
            case "application/cdmi-container": return "cdmic";
            case "application/cdmi-domain": return "cdmid";
            case "application/cdmi-object": return "cdmio";
            case "application/cdmi-queue": return "cdmiq";
            case "chemical/x-cdx": return "cdx";
            case "application/vnd.chemdraw+xml": return "cdxml";
            case "application/vnd.cinderella": return "cdy";
            case "application/pkix-cert": return "cer";
            case "application/x-cfs-compressed": return "cfs";
            case "image/cgm": return "cgm";
            case "application/x-chat": return "chat";
            case "application/vnd.ms-htmlhelp": return "chm";
            case "application/vnd.kde.kchart": return "chrt";
            case "chemical/x-cif": return "cif";
            case "application/vnd.anser-web-certificate-issue-initiation": return "cii";
            case "application/vnd.ms-artgalry": return "cil";
            case "application/vnd.claymore": return "cla";
            case "application/java-vm": return "class";
            case "application/vnd.crick.clicker.keyboard": return "clkk";
            case "application/vnd.crick.clicker.palette": return "clkp";
            case "application/vnd.crick.clicker.template": return "clkt";
            case "application/vnd.crick.clicker.wordbank": return "clkw";
            case "application/vnd.crick.clicker": return "clkx";
            case "application/x-msclip": return "clp";
            case "application/vnd.cosmocaller": return "cmc";
            case "chemical/x-cmdf": return "cmdf";
            case "chemical/x-cml": return "cml";
            case "application/vnd.yellowriver-custom-menu": return "cmp";
            case "image/x-cmx": return "cmx";
            case "application/vnd.rim.cod": return "cod";


            case "application/x-cpio": return "cpio";

            case "application/mac-compactpro": return "cpt";
            case "application/x-mscardfile": return "crd";
            case "application/pkix-crl": return "crl";
            case "application/x-x509-ca-cert": return "crt";
            case "application/vnd.rig.cryptonote": return "cryptonote";
            case "application/x-csh": return "csh";
            case "chemical/x-csml": return "csml";
            case "application/vnd.commonspace": return "csp";
            case "text/css": return "css";

            case "text/csv": return "csv";
            case "application/cu-seeme": return "cu";
            case "text/vnd.curl": return "curl";
            case "application/prs.cww": return "cww";

            case "text/x-c": return "cxx";
            case "model/vnd.collada+xml": return "dae";
            case "application/vnd.mobius.daf": return "daf";
            case "application/vnd.dart": return "dart";
            case "application/vnd.fdsn.seed": return "dataless";
            case "application/davmount+xml": return "davmount";
            case "application/docbook+xml": return "dbk";

            case "text/vnd.curl.dcurl": return "dcurl";
            case "application/vnd.oma.dd2+xml": return "dd2";
            case "application/vnd.fujixerox.ddd": return "ddd";
            case "application/x-debian-package": return "deb";



            case "application/vnd.dreamfactory": return "dfac";
            case "application/x-dgc-compressed": return "dgc";

            case "application/x-director": return "dir";
            case "application/vnd.mobius.dis": return "dis";

            case "image/vnd.djvu": return "djvu";

            case "application/x-apple-diskimage": return "dmg";
            case "application/vnd.tcpdump.pcap": return "dmp";

            case "application/vnd.dna": return "dna";
            case "application/msword": return "doc";
            case "application/vnd.ms-word.document.macroenabled.12": return "docm";
            case "application/vnd.openxmlformats-officedocument.wordprocessingml.document": return "docx";
            case "application/vnd.ms-word.template.macroenabled.12": return "dotm";
            case "application/vnd.openxmlformats-officedocument.wordprocessingml.template": return "dotx";
            case "application/vnd.osgi.dp": return "dp";
            case "application/vnd.dpgraph": return "dpg";
            case "audio/vnd.dra": return "dra";
            case "text/prs.lines.tag": return "dsc";
            case "application/dssc+der": return "dssc";
            case "application/x-dtbook+xml": return "dtb";
            case "application/xml-dtd": return "dtd";
            case "audio/vnd.dts": return "dts";
            case "audio/vnd.dts.hd": return "dtshd";
            case "video/vnd.dvb.file": return "dvb";
            case "application/x-dvi": return "dvi";
            case "model/vnd.dwf": return "dwf";
            case "image/vnd.dwg": return "dwg";
            case "image/vnd.dxf": return "dxf";
            case "application/vnd.spotfire.dxp": return "dxp";

            case "audio/vnd.nuera.ecelp4800": return "ecelp4800";
            case "audio/vnd.nuera.ecelp7470": return "ecelp7470";
            case "audio/vnd.nuera.ecelp9600": return "ecelp9600";

            case "application/vnd.novadigm.edm": return "edm";
            case "application/vnd.novadigm.edx": return "edx";
            case "application/vnd.picsel": return "efif";
            case "application/vnd.pg.osasli": return "ei6";

            case "message/rfc822": return "eml";
            case "application/emma+xml": return "emma";
            case "application/vnd.digital-winds": return "eol";
            case "application/vnd.ms-fontobject": return "eot";
            case "application/epub+zip": return "epub";
            case "application/ecmascript": return "es";
            case "application/vnd.eszigno3+xml": return "es3";
            case "application/vnd.osgi.subsystem": return "esa";
            case "application/vnd.epson.esf": return "esf";

            case "text/x-setext": return "etx";
            case "application/x-eva": return "eva";
            case "application/x-envoy": return "evy";
            case "application/x-msdownload": return "exe";
            case "application/exi": return "exi";
            case "application/vnd.novadigm.ext": return "ext";
            case "application/andrew-inset": return "ez";
            case "application/vnd.ezpix-album": return "ez2";
            case "application/vnd.ezpix-package": return "ez3";
            case "text/x-fortran": return "f";
            case "video/x-f4v": return "f4v";

            case "image/vnd.fastbidsheet": return "fbs";
            case "application/vnd.adobe.formscentral.fcdt": return "fcdt";
            case "application/vnd.isac.fcs": return "fcs";
            case "application/vnd.fdf": return "fdf";
            case "application/vnd.denovo.fcselayout-link": return "fe_launch";
            case "application/vnd.fujitsu.oasysgp": return "fg5";

            case "image/x-freehand": return "fhc";
            case "application/x-xfig": return "fig";
            case "audio/x-flac": return "flac";
            case "video/x-fli": return "fli";
            case "application/vnd.micrografx.flo": return "flo";
            case "video/x-flv": return "flv";
            case "application/vnd.kde.kivio": return "flw";
            case "text/vnd.fmi.flexstor": return "flx";
            case "text/vnd.fly": return "fly";
            case "application/vnd.framemaker": return "fm";
            case "application/vnd.frogans.fnc": return "fnc";
            case "image/vnd.fpx": return "fpx";

            case "application/vnd.fsc.weblaunch": return "fsc";
            case "image/vnd.fst": return "fst";
            case "application/vnd.fluxtime.clip": return "ftc";
            case "application/vnd.anser-web-funds-transfer-initiation": return "fti";
            case "video/vnd.fvt": return "fvt";
            case "application/vnd.adobe.fxp": return "fxp";
            case "application/vnd.fuzzysheet": return "fzs";
            case "application/vnd.geoplan": return "g2w";
            case "image/g3fax": return "g3";
            case "application/vnd.geospace": return "g3w";
            case "application/vnd.groove-account": return "gac";
            case "application/x-tads": return "gam";
            case "application/rpki-ghostbusters": return "gbr";
            case "application/x-gca-compressed": return "gca";
            case "model/vnd.gdl": return "gdl";
            case "application/vnd.dynageo": return "geo";
            case "application/vnd.geometry-explorer": return "gex";
            case "application/vnd.geogebra.file": return "ggb";
            case "application/vnd.geogebra.tool": return "ggt";
            case "application/vnd.groove-help": return "ghf";

            case "application/vnd.groove-identity-message": return "gim";
            case "application/gml+xml": return "gml";
            case "application/vnd.gmx": return "gmx";
            case "application/x-gnumeric": return "gnumeric";
            case "application/vnd.flographit": return "gph";
            case "application/gpx+xml": return "gpx";
            case "application/vnd.grafeq": return "gqf";
            case "application/srgs": return "gram";
            case "application/x-gramps-xml": return "gramps";

            case "application/vnd.groove-injector": return "grv";
            case "application/srgs+xml": return "grxml";
            case "application/x-font-ghostscript": return "gsf";
            case "application/x-gtar": return "gtar";
            case "application/vnd.groove-tool-message": return "gtm";
            case "model/vnd.gtw": return "gtw";
            case "text/vnd.graphviz": return "gv";
            case "application/gxf": return "gxf";
            case "application/vnd.geonext": return "gxt";

            case "video/h261": return "h261";
            case "video/h263": return "h263";
            case "video/h264": return "h264";
            case "application/vnd.hal+xml": return "hal";
            case "application/vnd.hbci": return "hbci";
            case "application/x-hdf": return "hdf";
            case "application/winhlp": return "hlp";
            case "application/vnd.hp-hpgl": return "hpgl";
            case "application/vnd.hp-hpid": return "hpid";
            case "application/vnd.hp-hps": return "hps";
            case "application/mac-binhex40": return "hqx";
            case "application/vnd.kenameaapp": return "htke";
            case "text/html": return "html";
            case "application/vnd.yamaha.hv-dic": return "hvd";
            case "application/vnd.yamaha.hv-voice": return "hvp";
            case "application/vnd.yamaha.hv-script": return "hvs";
            case "application/vnd.intergeo": return "i2g";
            case "application/vnd.iccprofile": return "icc";
            case "x-conference/x-cooltalk": return "ice";

            case "image/x-icon": return "ico";
            case "text/calendar": return "ics";
            case "image/ief": return "ief";

            case "application/vnd.shana.informed.formdata": return "ifm";

            case "application/vnd.igloader": return "igl";
            case "application/vnd.insors.igm": return "igm";
            case "model/iges": return "igs";
            case "application/vnd.micrografx.igx": return "igx";
            case "application/vnd.shana.informed.interchange": return "iif";
            case "application/vnd.accpac.simply.imp": return "imp";
            case "application/vnd.ms-ims": return "ims";

            case "application/inkml+xml": return "inkml";
            case "application/x-install-instructions": return "install";
            case "application/vnd.astraea-software.iota": return "iota";
            case "application/ipfix": return "ipfix";
            case "application/vnd.shana.informed.package": return "ipk";
            case "application/vnd.ibm.rights-management": return "irm";
            case "application/vnd.irepository.package+xml": return "irp";

            case "application/vnd.shana.informed.formtemplate": return "itp";
            case "application/vnd.immervision-ivp": return "ivp";
            case "application/vnd.immervision-ivu": return "ivu";
            case "text/vnd.sun.j2me.app-descriptor": return "jad";
            case "application/vnd.jam": return "jam";
            case "application/java-archive": return "jar";
            case "text/x-java-source": return "java";
            case "application/vnd.jisp": return "jisp";
            case "application/vnd.hp-jlyt": return "jlt";
            case "application/x-java-jnlp-file": return "jnlp";
            case "application/vnd.joost.joda-archive": return "joda";

            case "video/jpeg": return "jpgv";
            case "video/jpm": return "jpm";
            case "application/javascript": return "js";
            case "application/json": return "json";
            case "application/jsonml+json": return "jsonml";

            case "application/vnd.kde.karbon": return "karbon";
            case "application/vnd.kde.kformula": return "kfo";
            case "application/vnd.kidspiration": return "kia";
            case "application/vnd.google-earth.kml+xml": return "kml";
            case "application/vnd.google-earth.kmz": return "kmz";
            case "application/vnd.kinar": return "kne";
            case "application/vnd.kde.kontour": return "kon";
            case "application/vnd.kde.kpresenter": return "kpr";
            case "application/vnd.ds-keypoint": return "kpxx";
            case "application/vnd.kde.kspread": return "ksp";
            case "application/vnd.kahootz": return "ktr";
            case "image/ktx": return "ktx";
            case "application/vnd.kde.kword": return "kwd";
            case "application/vnd.las.las+xml": return "lasxml";
            case "application/x-latex": return "latex";
            case "application/vnd.llamagraphics.life-balance.desktop": return "lbd";
            case "application/vnd.llamagraphics.life-balance.exchange+xml": return "lbe";
            case "application/vnd.hhe.lesson-player": return "les";

            case "application/vnd.route66.link66+xml": return "link66";


            case "application/x-ms-shortcut": return "lnk";

            case "application/lost+xml": return "lostxml";

            case "application/vnd.ms-lrm": return "lrm";
            case "application/vnd.frogans.ltf": return "ltf";
            case "audio/vnd.lucent.voice": return "lvp";
            case "application/vnd.lotus-wordpro": return "lwp";

            case "application/x-msmediaview": return "m13";
            case "video/mpeg": return "m1v";
            case "application/mp21": return "m21";

            case "audio/x-mpegurl": return "m3u";
            case "application/vnd.apple.mpegurl": return "m3u8";
            case "audio/mp4": return "m4a";
            case "video/x-m4v": return "m4v";
            case "application/mathematica": return "ma";
            case "application/mads+xml": return "mads";
            case "application/vnd.ecowin.chart": return "mag";

            case "text/troff": return "trof";

            case "application/mathml+xml": return "mathml";

            case "application/vnd.mobius.mbk": return "mbk";
            case "application/mbox": return "mbox";
            case "application/vnd.medcalcdata": return "mc1";
            case "application/vnd.mcd": return "mcd";
            case "text/vnd.curl.mcurl": return "mcurl";
            case "application/x-msaccess": return "mdb";
            case "image/vnd.ms-modi": return "mdi";
            case "model/mesh": return "mesh";
            case "application/metalink4+xml": return "meta4";
            case "application/mets+xml": return "mets";
            case "application/vnd.mfmp": return "mfm";
            case "application/rpki-manifest": return "mft";
            case "application/vnd.osgeo.mapguide.package": return "mgp";
            case "audio/midi": return "mid";
            case "application/x-mie": return "mie";
            case "application/vnd.mif": return "mif";
            case "video/mj2": return "mj2";
            case "video/x-matroska": return "mkv";
            case "application/vnd.dolby.mlp": return "mlp";
            case "application/vnd.chipnuts.karaoke-mmd": return "mmd";
            case "application/vnd.smaf": return "mmf";
            case "image/vnd.fujixerox.edmics-mmr": return "mmr";
            case "video/x-mng": return "mng";
            case "application/x-msmoney": return "mny";
            case "application/x-mobipocket-ebook": return "mobi";
            case "application/mods+xml": return "mods";
            case "video/quicktime": return "mov";
            case "video/x-sgi-movie": return "movie";
            case "audio/mpeg": return "mp3";
            case "video/mp4": return "mp4";
            case "application/vnd.mophun.certificate": return "mpc";

            case "application/vnd.apple.installer+xml": return "mpkg";
            case "application/vnd.blueice.multipass": return "mpm";
            case "application/vnd.mophun.application": return "mpn";
            case "application/vnd.ms-project": return "mpt";
            case "application/vnd.ibm.minipay": return "mpy";
            case "application/vnd.mobius.mqy": return "mqy";
            case "application/marc": return "mrc";
            case "application/marcxml+xml": return "mrcx";

            case "application/mediaservercontrol+xml": return "mscml";
            case "application/vnd.fdsn.mseed": return "mseed";
            case "application/vnd.mseq": return "mseq";
            case "application/vnd.epson.msf": return "msf";


            case "application/vnd.mobius.msl": return "msl";
            case "application/vnd.muvee.style": return "msty";
            case "model/vnd.mts": return "mts";
            case "application/vnd.musician": return "mus";
            case "application/vnd.recordare.musicxml+xml": return "musicxml";
            case "application/vnd.mfer": return "mwf";
            case "application/mxf": return "mxf";
            case "application/vnd.recordare.musicxml": return "mxl";
            case "application/xv+xml": return "mxml";
            case "application/vnd.triscape.mxs": return "mxs";
            case "video/vnd.mpegurl": return "mxu";
            case "application/vnd.nokia.n-gage.symbian.install": return "n-gage";
            case "text/n3": return "n3";

            case "application/vnd.wolfram.player": return "nbp";
            case "application/x-netcdf": return "nc";
            case "application/x-dtbncx+xml": return "ncx";
            case "text/x-nfo": return "nfo";
            case "application/vnd.nokia.n-gage.data": return "ngdat";
            case "application/vnd.nitf": return "nitf";
            case "application/vnd.neurolanguage.nlu": return "nlu";
            case "application/vnd.enliven": return "nml";
            case "application/vnd.noblenet-directory": return "nnd";
            case "application/vnd.noblenet-sealer": return "nns";
            case "application/vnd.noblenet-web": return "nnw";
            case "image/vnd.net-fpx": return "npx";
            case "application/x-conference": return "nsc";
            case "application/vnd.lotus-notes": return "nsf";

            case "application/x-nzb": return "nzb";
            case "application/vnd.fujitsu.oasys2": return "oa2";
            case "application/vnd.fujitsu.oasys3": return "oa3";
            case "application/vnd.fujitsu.oasys": return "oas";
            case "application/x-msbinder": return "obd";
            case "application/x-tgif": return "obj";
            case "application/oda": return "oda";
            case "application/vnd.oasis.opendocument.database": return "odb";
            case "application/vnd.oasis.opendocument.chart": return "odc";
            case "application/vnd.oasis.opendocument.formula": return "odf";
            case "application/vnd.oasis.opendocument.formula-template": return "odft";
            case "application/vnd.oasis.opendocument.graphics": return "odg";
            case "application/vnd.oasis.opendocument.image": return "odi";
            case "application/vnd.oasis.opendocument.text-master": return "odm";
            case "application/vnd.oasis.opendocument.presentation": return "odp";
            case "application/vnd.oasis.opendocument.spreadsheet": return "ods";
            case "application/vnd.oasis.opendocument.text": return "odt";
            case "audio/ogg": return "oga";
            case "video/ogg": return "ogv";
            case "application/ogg": return "ogx";
            case "application/omdoc+xml": return "omdoc";
            case "application/onenote": return "onetoc";
            case "application/oebps-package+xml": return "opf";
            case "text/x-opml": return "opml";

            case "application/vnd.lotus-organizer": return "org";
            case "application/vnd.yamaha.openscoreformat": return "osf";
            case "application/vnd.yamaha.openscoreformat.osfpvg+xml": return "osfpvg";
            case "application/vnd.oasis.opendocument.chart-template": return "otc";
            case "font/otf": return "otf";
            case "application/vnd.oasis.opendocument.graphics-template": return "otg";
            case "application/vnd.oasis.opendocument.text-web": return "oth";
            case "application/vnd.oasis.opendocument.image-template": return "oti";
            case "application/vnd.oasis.opendocument.presentation-template": return "otp";
            case "application/vnd.oasis.opendocument.spreadsheet-template": return "ots";
            case "application/vnd.oasis.opendocument.text-template": return "ott";
            case "application/oxps": return "oxps";
            case "application/vnd.openofficeorg.extension": return "oxt";
            case "text/x-pascal": return "p";
            case "application/pkcs10": return "p10";
            case "application/x-pkcs7-certificates": return "p7b";
            case "application/pkcs7-mime": return "p7m";
            case "application/x-pkcs7-certreqresp": return "p7r";
            case "application/pkcs7-signature": return "p7s";
            case "application/pkcs8": return "p8";

            case "application/vnd.pawaafile": return "paw";
            case "application/vnd.powerbuilder6": return "pbd";
            case "image/x-portable-bitmap": return "pbm";

            case "application/x-font-pcf": return "pcf";
            case "application/vnd.hp-pcl": return "pcl";
            case "image/x-pict": return "pct";
            case "application/vnd.curl.pcurl": return "pcurl";
            case "image/x-pcx": return "pcx";


            case "application/x-font-type1": return "pfm";
            case "application/font-tdpfr": return "pfr";
            case "application/x-pkcs12": return "pfx";
            case "image/x-portable-graymap": return "pgm";
            case "application/x-chess-pgn": return "pgn";
            case "application/pgp-encrypted": return "pgp";


            case "application/pkixcmp": return "pki";
            case "application/pkix-pkipath": return "pkipath";
            case "application/vnd.3gpp.pic-bw-large": return "plb";
            case "application/vnd.mobius.plc": return "plc";
            case "application/vnd.pocketlearn": return "plf";
            case "application/pls+xml": return "pls";
            case "application/vnd.ctc-posml": return "pml";

            case "image/x-portable-anymap": return "pnm";
            case "application/vnd.macports.portpkg": return "portpkg";
            case "application/vnd.ms-powerpoint": return "pps";
            case "application/vnd.ms-powerpoint.template.macroenabled.12": return "potm";
            case "application/vnd.openxmlformats-officedocument.presentationml.template": return "potx";
            case "application/vnd.ms-powerpoint.addin.macroenabled.12": return "ppam";
            case "application/vnd.cups-ppd": return "ppd";
            case "image/x-portable-pixmap": return "ppm";
            case "application/vnd.ms-powerpoint.slideshow.macroenabled.12": return "ppsm";
            case "application/vnd.openxmlformats-officedocument.presentationml.slideshow": return "ppsx";
            case "application/vnd.ms-powerpoint.presentation.macroenabled.12": return "pptm";
            case "application/vnd.openxmlformats-officedocument.presentationml.presentation": return "pptx";
            case "application/vnd.palm": return "pqa";
            case "application/vnd.lotus-freelance": return "pre";
            case "application/pics-rules": return "prf";
            case "application/postscript": return "eps";
            case "image/vnd.adobe.photoshop": return "psd";
            case "application/x-font-linux-psf": return "psf";
            case "application/pskc+xml": return "pskcxml";
            case "application/vnd.pvi.ptid1": return "ptid";
            case "application/x-mspublisher": return "pub";
            case "application/vnd.3m.post-it-notes": return "pwn";
            case "audio/vnd.ms-playready.media.pya": return "pya";
            case "video/vnd.ms-playready.media.pyv": return "pyv";
            case "application/vnd.epson.quickanime": return "qam";
            case "application/vnd.intu.qbo": return "qbo";
            case "application/vnd.intu.qfx": return "qfx";
            case "application/vnd.publishare-delta-tree": return "qps";

            case "application/vnd.quark.quarkxpress": return "qxd";
            case "audio/x-pn-realaudio": return "ra";
            case "application/x-rar-compressed": return "rar";
            case "image/x-cmu-raster": return "ras";
            case "application/vnd.ipunplugged.rcprofile": return "rcprofile";
            case "application/rdf+xml": return "rdf";
            case "application/vnd.data-vision.rdz": return "rdz";
            case "application/vnd.businessobjects": return "rep";
            case "application/x-dtbresource+xml": return "res";
            case "image/x-rgb": return "rgb";
            case "application/reginfo+xml": return "rif";
            case "audio/vnd.rip": return "rip";
            case "application/x-research-info-systems": return "ris";
            case "application/resource-lists+xml": return "rl";
            case "image/vnd.fujixerox.edmics-rlc": return "rlc";
            case "application/resource-lists-diff+xml": return "rld";
            case "application/vnd.rn-realmedia": return "rm";

            case "audio/x-pn-realaudio-plugin": return "rmp";
            case "application/vnd.jcp.javame.midlet-rms": return "rms";
            case "application/vnd.rn-realmedia-vbr": return "rmvb";
            case "application/relax-ng-compact-syntax": return "rnc";
            case "application/rpki-roa": return "roa";

            case "application/vnd.cloanto.rp9": return "rp9";
            case "application/vnd.nokia.radio-presets": return "rpss";
            case "application/vnd.nokia.radio-preset": return "rpst";
            case "application/sparql-query": return "rq";
            case "application/rls-services+xml": return "rs";
            case "application/rsd+xml": return "rsd";
            case "application/rss+xml": return "rss";
            case "application/rtf": return "rtf";
            case "text/richtext": return "rtx";
            case "audio/s3m": return "s3m";
            case "application/vnd.yamaha.smaf-audio": return "saf";
            case "application/sbml+xml": return "sbml";
            case "application/vnd.ibm.secure-container": return "sc";
            case "application/x-msschedule": return "scd";
            case "application/vnd.lotus-screencam": return "scm";
            case "application/scvp-cv-request": return "scq";
            case "application/scvp-cv-response": return "scs";
            case "text/vnd.curl.scurl": return "scurl";
            case "application/vnd.stardivision.draw": return "sda";
            case "application/vnd.stardivision.calc": return "sdc";
            case "application/vnd.stardivision.impress": return "sdd";
            case "application/vnd.solent.sdkm+xml": return "sdkm";
            case "application/sdp": return "sdp";
            case "application/vnd.stardivision.writer": return "sdw";
            case "application/vnd.seemail": return "see";
            case "application/vnd.sema": return "sema";
            case "application/vnd.semd": return "semd";
            case "application/vnd.semf": return "semf";
            case "application/java-serialized-object": return "ser";
            case "application/set-payment-initiation": return "setpay";
            case "application/set-registration-initiation": return "setreg";
            case "application/vnd.hydrostatix.sof-data": return "sfd-hdstx";
            case "application/vnd.spotfire.sfs": return "sfs";
            case "text/x-sfv": return "sfv";
            case "image/sgi": return "sgi";
            case "application/vnd.stardivision.writer-global": return "sgl";
            case "text/sgml": return "sgml";
            case "application/x-sh": return "sh";
            case "application/x-shar": return "shar";
            case "application/shf+xml": return "shf";
            case "image/x-mrsid-image": return "sid";
            case "application/pgp-signature": return "sig";
            case "audio/silk": return "sil";

            case "application/vnd.symbian.install": return "sis";
            case "application/x-stuffit": return "sit";
            case "application/x-stuffitx": return "sitx";
            case "application/sieve": return "siv";
            case "application/vnd.koan": return "skp";
            case "application/vnd.ms-powerpoint.slide.macroenabled.12": return "sldm";
            case "application/vnd.openxmlformats-officedocument.presentationml.slide": return "sldx";
            case "application/vnd.epson.salt": return "slt";
            case "application/vnd.stepmania.stepchart": return "sm";
            case "application/vnd.stardivision.math": return "smf";
            case "application/smil+xml": return "smil";
            case "video/x-smv": return "smv";
            case "application/vnd.stepmania.package": return "smzip";
            case "application/x-font-snf": return "snf";
            case "application/vnd.yamaha.smaf-phrase": return "spf";
            case "application/x-futuresplash": return "spl";
            case "text/vnd.in3d.spot": return "spot";
            case "application/scvp-vp-response": return "spp";
            case "application/scvp-vp-request": return "spq";
            case "application/x-sql": return "sql";
            case "application/x-wais-source": return "src";
            case "application/x-subrip": return "srt";
            case "application/sru+xml": return "sru";
            case "application/sparql-results+xml": return "srx";
            case "application/ssdl+xml": return "ssdl";
            case "application/vnd.kodak-descriptor": return "sse";
            case "application/vnd.epson.ssf": return "ssf";
            case "application/ssml+xml": return "ssml";
            case "application/vnd.sailingtracker.track": return "st";
            case "application/vnd.sun.xml.calc.template": return "stc";
            case "application/vnd.sun.xml.draw.template": return "std";
            case "application/vnd.wt.stf": return "stf";
            case "application/vnd.sun.xml.impress.template": return "sti";
            case "application/hyperstudio": return "stk";
            case "application/vnd.ms-pki.stl": return "stl";
            case "application/vnd.pg.format": return "str";
            case "application/vnd.sun.xml.writer.template": return "stw";
            case "image/vnd.dvb.subtitle": return "sub";
            case "application/vnd.sus-calendar": return "sus";
            case "application/x-sv4cpio": return "sv4cpio";
            case "application/x-sv4crc": return "sv4crc";
            case "application/vnd.dvb.service": return "svc";
            case "application/vnd.svd": return "svd";


            case "application/x-shockwave-flash": return "swf";
            case "application/vnd.aristanetworks.swi": return "swi";
            case "application/vnd.sun.xml.calc": return "sxc";
            case "application/vnd.sun.xml.draw": return "sxd";
            case "application/vnd.sun.xml.writer.global": return "sxg";
            case "application/vnd.sun.xml.impress": return "sxi";
            case "application/vnd.sun.xml.math": return "sxm";
            case "application/vnd.sun.xml.writer": return "sxw";

            case "application/x-t3vm-image": return "t3";
            case "application/vnd.mynfc": return "taglet";
            case "application/vnd.tao.intent-module-archive": return "tao";
            case "application/x-tar": return "tar";
            case "application/vnd.3gpp2.tcap": return "tcap";
            case "application/x-tcl": return "tcl";
            case "application/vnd.smart.teacher": return "teacher";
            case "application/tei+xml": return "tei";
            case "application/x-tex": return "tex";
            case "application/x-texinfo": return "texinfo";
            case "application/thraud+xml": return "tfi";
            case "application/x-tex-tfm": return "tfm";
            case "image/x-tga": return "tga";
            case "application/vnd.ms-officetheme": return "thmx";
            case "image/tiff": return "tiff";
            case "application/vnd.tmobile-livetv": return "tmo";
            case "application/x-bittorrent": return "torrent";
            case "application/vnd.groove-tool-template": return "tpl";
            case "application/vnd.trid.tpt": return "tpt";
            case "application/vnd.trueapp": return "tra";
            case "application/x-msterminal": return "trm";
            case "application/timestamped-data": return "tsd";
            case "text/tab-separated-values": return "tsv";
            case "font/collection": return "ttc";
            case "font/ttf": return "ttf";
            case "text/turtle": return "ttl";
            case "application/vnd.simtech-mindmapper": return "twd";
            case "application/vnd.genomatix.tuxedo": return "txd";
            case "application/vnd.mobius.txf": return "txf";
            case "text/plain": return "txt";


            case "application/vnd.ufdl": return "ufdl";
            case "application/x-glulx": return "ulx";
            case "application/vnd.umajin": return "umj";
            case "application/vnd.unity": return "unityweb";
            case "application/vnd.uoml+xml": return "uoml";
            case "text/uri-list": return "uri";
            case "application/x-ustar": return "ustar";
            case "application/vnd.uiq.theme": return "utz";
            case "text/x-uuencode": return "uu";
            case "audio/vnd.dece.audio": return "uva";
            case "application/vnd.dece.data": return "uvd";
            case "image/vnd.dece.graphic": return "uvg";
            case "video/vnd.dece.hd": return "uvh";
            case "video/vnd.dece.mobile": return "uvm";
            case "video/vnd.dece.pd": return "uvp";
            case "video/vnd.dece.sd": return "uvs";
            case "application/vnd.dece.ttml+xml": return "uvt";
            case "video/vnd.uvvu.mp4": return "uvu";
            case "video/vnd.dece.video": return "uvv";
            case "application/vnd.dece.unspecified": return "uvx";
            case "application/vnd.dece.zip": return "uvz";
            case "text/vcard": return "vcard";
            case "application/x-cdlink": return "vcd";
            case "text/x-vcard": return "vcf";
            case "application/vnd.groove-vcard": return "vcg";
            case "text/x-vcalendar": return "vcs";
            case "application/vnd.vcx": return "vcx";
            case "application/vnd.visionary": return "vis";
            case "video/vnd.vivo": return "viv";
            case "video/x-ms-vob": return "vob";


            case "model/vrml": return "vrml";
            case "application/vnd.visio": return "vsd";
            case "application/vnd.vsf": return "vsf";
            case "model/vnd.vtu": return "vtu";
            case "application/voicexml+xml": return "vxml";

            case "application/x-doom": return "wad";
            case "audio/x-wav": return "wav";
            case "audio/x-ms-wax": return "wax";
            case "image/vnd.wap.wbmp": return "wbmp";
            case "application/vnd.criticaltools.wbs+xml": return "wbs";
            case "application/vnd.wap.wbxml": return "wbxml";
            case "application/vnd.ms-works": return "wks";
            case "image/vnd.ms-photo": return "wdp";
            case "audio/webm": return "weba";
            case "video/webm": return "webm";
            case "image/webp": return "webp";
            case "application/vnd.pmi.widget": return "wg";
            case "application/widget": return "wgt";

            case "video/x-ms-wm": return "wm";
            case "audio/x-ms-wma": return "wma";
            case "application/x-ms-wmd": return "wmd";
            case "application/x-msmetafile": return "wmf";
            case "text/vnd.wap.wml": return "wml";
            case "application/vnd.wap.wmlc": return "wmlc";
            case "text/vnd.wap.wmlscript": return "wmls";
            case "application/vnd.wap.wmlscriptc": return "wmlsc";
            case "video/x-ms-wmv": return "wmv";
            case "video/x-ms-wmx": return "wmx";

            case "font/woff": return "woff";
            case "font/woff2": return "woff2";
            case "application/vnd.wordperfect": return "wpd";
            case "application/vnd.ms-wpl": return "wpl";
            case "application/vnd.wqd": return "wqd";
            case "application/x-mswrite": return "wri";

            case "application/wsdl+xml": return "wsdl";
            case "application/wspolicy+xml": return "wspolicy";
            case "application/vnd.webturbo": return "wtb";
            case "video/x-ms-wvx": return "wvx";

            case "model/x3d+xml": return "x3d";
            case "application/xaml+xml": return "xaml";
            case "application/x-silverlight-app": return "xap";
            case "application/vnd.xara": return "xar";
            case "application/x-ms-xbap": return "xbap";
            case "application/vnd.fujixerox.docuworks.binder": return "xbd";
            case "image/x-xbitmap": return "xbm";
            case "application/xcap-diff+xml": return "xdf";
            case "application/vnd.syncml.dm+xml": return "xdm";
            case "application/vnd.adobe.xdp+xml": return "xdp";
            case "application/dssc+xml": return "xdssc";
            case "application/vnd.fujixerox.docuworks": return "xdw";
            case "application/xenc+xml": return "xenc";
            case "application/patch-ops-error+xml": return "xer";
            case "application/vnd.adobe.xfdf": return "xfdf";
            case "application/vnd.xfdl": return "xfdl";
            case "application/xhtml+xml": return "xhtml";

            case "image/vnd.xiff": return "xif";
            case "application/vnd.ms-excel": return "xls";
            case "application/vnd.ms-excel.sheet.binary.macroenabled.12": return "xlsb";
            case "application/vnd.ms-excel.sheet.macroenabled.12": return "xlsm";
            case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": return "xlsx";
            case "application/vnd.ms-excel.template.macroenabled.12": return "xltm";
            case "application/vnd.openxmlformats-officedocument.spreadsheetml.template": return "xltx";
            case "application/xml": return "xml";
            case "application/vnd.olpc-sugar": return "xo";
            case "application/xop+xml": return "xop";
            case "application/x-xpinstall": return "xpi";
            case "application/xproc+xml": return "xpl";
            case "image/x-xpixmap": return "xpm";
            case "application/vnd.is-xpr": return "xpr";
            case "application/vnd.ms-xpsdocument": return "xps";
            case "application/vnd.intercon.formnet": return "xpw";
            case "application/xslt+xml": return "xslt";
            case "application/vnd.syncml+xml": return "xsm";
            case "application/xspf+xml": return "xspf";
            case "application/vnd.mozilla.xul+xml": return "xul";
            case "image/x-xwindowdump": return "xwd";
            case "chemical/x-xyz": return "xyz";
            case "application/x-xz": return "xz";
            case "text/yaml": return "yaml";
            case "application/yang": return "yang";
            case "application/yin+xml": return "yin";
            case "application/vnd.zzazz.deck+xml": return "zaz";
            case "application/zip": return "zip";
            case "application/vnd.zul": return "zir";
            case "application/vnd.handheld-entertainment+xml": return "zmm";
            case "application/zstd": return "zst";
            default: return "";
        }
    }


}
