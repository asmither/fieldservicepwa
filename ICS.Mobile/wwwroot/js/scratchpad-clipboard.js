// ScratchPad Clipboard Utilities

window.scratchPadClipboard = {
    
    // Read clipboard and return both text and image if available
    read: async function() {
        const result = {
            text: null,
            imageBase64: null,
            imageMimeType: null
        };
        
        try {
            // Try to read clipboard items (modern API)
            if (navigator.clipboard && navigator.clipboard.read) {
                const items = await navigator.clipboard.read();
                
                for (const item of items) {
                    // Check for image types
                    for (const type of item.types) {
                        if (type.startsWith('image/')) {
                            const blob = await item.getType(type);
                            const rawBase64 = await this.blobToBase64(blob);
                            // Resize if needed
                            const resized = await this.resizeImage(rawBase64, type);
                            result.imageBase64 = resized.base64;
                            result.imageMimeType = resized.mimeType;
                        } else if (type === 'text/plain') {
                            const blob = await item.getType(type);
                            result.text = await blob.text();
                        }
                    }
                }
            } else {
                // Fallback to text-only for older browsers
                result.text = await navigator.clipboard.readText();
            }
        } catch (err) {
            console.warn('Clipboard read error:', err);
            // Try text-only fallback
            try {
                result.text = await navigator.clipboard.readText();
            } catch (e) {
                console.warn('Text clipboard fallback failed:', e);
            }
        }
        
        return result;
    },
    
    // Write image to clipboard (falls back to share on iOS)
    writeImage: async function(base64Data, mimeType) {
        // Detect iOS
        const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) || 
                      (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
        
        // On iOS, clipboard write for images doesn't work well - use share instead
        if (isIOS) {
            try {
                const shared = await window.scratchPadShareImage(base64Data, mimeType);
                return shared ? 'shared' : 'failed';
            } catch (err) {
                console.log('iOS share fallback failed:', err);
                return 'failed';
            }
        }
        
        // Standard clipboard write for other browsers
        try {
            // Clipboard API only supports PNG - convert if needed
            let pngBlob;
            
            if (mimeType === 'image/png') {
                // Already PNG, just convert base64 to blob
                const byteCharacters = atob(base64Data);
                const byteNumbers = new Array(byteCharacters.length);
                for (let i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }
                const byteArray = new Uint8Array(byteNumbers);
                pngBlob = new Blob([byteArray], { type: 'image/png' });
            } else {
                // Convert to PNG via canvas
                pngBlob = await this.convertToPng(base64Data, mimeType);
            }
            
            // Write to clipboard
            const clipboardItem = new ClipboardItem({
                'image/png': pngBlob
            });
            await navigator.clipboard.write([clipboardItem]);
            return 'copied';
        } catch (err) {
            console.error('Failed to copy image to clipboard:', err);
            return 'failed';
        }
    },
    
    // Convert any image to PNG blob via canvas
    convertToPng: function(base64Data, mimeType) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = function() {
                const canvas = document.createElement('canvas');
                canvas.width = img.width;
                canvas.height = img.height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                canvas.toBlob(function(blob) {
                    resolve(blob);
                }, 'image/png');
            };
            img.onerror = reject;
            img.src = 'data:' + mimeType + ';base64,' + base64Data;
        });
    },
    
    // Helper: Convert blob to base64
    blobToBase64: function(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => {
                // Remove the data:mime;base64, prefix
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    },
    
    // Resize image if too large (returns {base64, mimeType})
    // maxDimension: largest width or height allowed (default 1280px)
    // maxSizeKB: max file size in KB (default 500KB)
    resizeImage: function(base64Data, mimeType, maxDimension = 1280, maxSizeKB = 500) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = function() {
                let width = img.width;
                let height = img.height;
                
                // Check if resize needed based on dimensions
                const needsResize = width > maxDimension || height > maxDimension;
                
                // Check approximate size (base64 is ~33% larger than binary)
                const approxSizeKB = (base64Data.length * 0.75) / 1024;
                const needsCompress = approxSizeKB > maxSizeKB;
                
                if (!needsResize && !needsCompress) {
                    // Already small enough
                    resolve({ base64: base64Data, mimeType: mimeType });
                    return;
                }
                
                // Calculate new dimensions
                if (needsResize) {
                    if (width > height) {
                        height = Math.round(height * (maxDimension / width));
                        width = maxDimension;
                    } else {
                        width = Math.round(width * (maxDimension / height));
                        height = maxDimension;
                    }
                }
                
                // Draw to canvas
                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, width, height);
                
                // Determine output format and quality
                // Use JPEG for photos (smaller), PNG for screenshots/graphics
                let outputType = 'image/jpeg';
                let quality = 0.85;
                
                // If original was PNG and small, keep as PNG
                if (mimeType === 'image/png' && !needsCompress) {
                    outputType = 'image/png';
                    quality = undefined;
                }
                
                // Convert to base64
                const dataUrl = canvas.toDataURL(outputType, quality);
                const newBase64 = dataUrl.split(',')[1];
                
                console.log(`Image resized: ${img.width}x${img.height} -> ${width}x${height}, ~${Math.round(approxSizeKB)}KB -> ~${Math.round((newBase64.length * 0.75) / 1024)}KB`);
                
                resolve({ base64: newBase64, mimeType: outputType });
            };
            img.onerror = reject;
            img.src = 'data:' + mimeType + ';base64,' + base64Data;
        });
    }
};

// Focus helper for edit mode
window.scratchPadFocusElement = function(elementId) {
    setTimeout(function() {
        var elem = document.getElementById(elementId);
        if (elem) {
            elem.focus();
            // Move cursor to end of text
            if (elem.setSelectionRange) {
                var len = elem.value.length;
                elem.setSelectionRange(len, len);
            }
        }
    }, 50); // Small delay to ensure element is rendered
};

// Share image using native Share API (opens share sheet on mobile)
window.scratchPadShareImage = async function(base64Data, mimeType) {
    try {
        // Check if Share API with files is supported
        if (!navigator.canShare) {
            return false;
        }
        
        // Convert base64 to blob
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: mimeType });
        
        // Create a file from blob
        const ext = mimeType.split('/')[1] || 'png';
        const fileName = 'scratchpad-image-' + Date.now() + '.' + ext;
        const file = new File([blob], fileName, { type: mimeType });
        
        // Check if we can share this file
        if (!navigator.canShare({ files: [file] })) {
            return false;
        }
        
        // Open share sheet
        await navigator.share({
            files: [file],
            title: 'Save Image'
        });
        
        return true;
    } catch (err) {
        // User cancelled or error
        console.log('Share cancelled or failed:', err);
        return false;
    }
};

// Download image fallback (for browsers without Share API)
window.scratchPadDownloadImage = function(base64Data, mimeType) {
    try {
        // Create data URL
        const dataUrl = 'data:' + mimeType + ';base64,' + base64Data;
        
        // Create download link
        const ext = mimeType.split('/')[1] || 'png';
        const fileName = 'scratchpad-image-' + Date.now() + '.' + ext;
        
        const link = document.createElement('a');
        link.href = dataUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (err) {
        console.error('Download failed:', err);
    }
};

// Photo picker - setup change handler (only once)
window.scratchPadSetupPhotoPicker = function(inputId, dotNetRef) {
    const input = document.getElementById(inputId);
    if (!input || input._scratchPadSetup) return; // Already setup
    
    input._scratchPadSetup = true;
    
    input.addEventListener('change', async function(e) {
        const file = e.target.files[0];
        if (!file) return;
        
        try {
            // Read file as base64
            const rawBase64 = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => {
                    // Remove the data:mime;base64, prefix
                    const result = reader.result.split(',')[1];
                    resolve(result);
                };
                reader.onerror = reject;
                reader.readAsDataURL(file);
            });
            
            // Resize if needed
            const resized = await window.scratchPadClipboard.resizeImage(rawBase64, file.type);
            
            // Call back to Blazor with resized image
            await dotNetRef.invokeMethodAsync('OnPhotoSelected', resized.base64, resized.mimeType);
            
            // Clear the input for next use
            input.value = '';
        } catch (err) {
            console.error('Photo read error:', err);
        }
    });
};

// Photo picker - trigger click
window.scratchPadTriggerPhotoPicker = function(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.click();
    }
};
