/**
 * Aashana Fashion — Face Recognition & Attendance Engine
 * Powered by face-api.js (TensorFlow.js)
 */

const FaceAttendanceEngine = {
    modelsLoaded: false,
    modelPath: 'https://cdn.jsdelivr.net/npm/@vladmandic/face-api/model/',
    // Alternate CDN fallback: 'https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights/'
    
    // Web Audio Synthesizer
    audioCtx: null,

    initAudio() {
        if (!this.audioCtx) {
            this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        }
        if (this.audioCtx.state === 'suspended') {
            this.audioCtx.resume();
        }
    },

    playSuccessSound() {
        try {
            this.initAudio();
            const now = this.audioCtx.currentTime;
            
            // Note 1 (E5)
            const osc1 = this.audioCtx.createOscillator();
            const gain1 = this.audioCtx.createGain();
            osc1.type = 'sine';
            osc1.frequency.setValueAtTime(659.25, now);
            gain1.gain.setValueAtTime(0.15, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.15);
            osc1.connect(gain1);
            gain1.connect(this.audioCtx.destination);
            osc1.start(now);
            osc1.stop(now + 0.15);

            // Note 2 (A5)
            const osc2 = this.audioCtx.createOscillator();
            const gain2 = this.audioCtx.createGain();
            osc2.type = 'sine';
            osc2.frequency.setValueAtTime(880.00, now + 0.1);
            gain2.gain.setValueAtTime(0.2, now + 0.1);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
            osc2.connect(gain2);
            gain2.connect(this.audioCtx.destination);
            osc2.start(now + 0.1);
            osc2.stop(now + 0.35);
        } catch (e) {
            console.warn('Audio feedback failed', e);
        }
    },

    playErrorSound() {
        try {
            this.initAudio();
            const now = this.audioCtx.currentTime;
            const osc = this.audioCtx.createOscillator();
            const gain = this.audioCtx.createGain();
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(220, now);
            gain.gain.setValueAtTime(0.15, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
            osc.connect(gain);
            gain.connect(this.audioCtx.destination);
            osc.start(now);
            osc.stop(now + 0.3);
        } catch (e) {
            console.warn('Audio error feedback failed', e);
        }
    },

    // Load Neural Network models
    async loadModels(onStatus) {
        if (this.modelsLoaded) return true;
        try {
            if (onStatus) onStatus('Loading face detection & neural models...');
            
            // Try VladMandic face-api model repository
            await faceapi.nets.tinyFaceDetector.loadFromUri(this.modelPath);
            await faceapi.nets.faceLandmark68Net.loadFromUri(this.modelPath);
            await faceapi.nets.faceRecognitionNet.loadFromUri(this.modelPath);
            
            this.modelsLoaded = true;
            if (onStatus) onStatus('Models loaded successfully.');
            return true;
        } catch (err) {
            console.warn('Primary model load failed, trying github raw weights...', err);
            try {
                const fallbackUrl = 'https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights/';
                await faceapi.nets.tinyFaceDetector.loadFromUri(fallbackUrl);
                await faceapi.nets.faceLandmark68Net.loadFromUri(fallbackUrl);
                await faceapi.nets.faceRecognitionNet.loadFromUri(fallbackUrl);
                this.modelsLoaded = true;
                if (onStatus) onStatus('Models loaded from backup mirror.');
                return true;
            } catch (err2) {
                console.error('All model loads failed: ', err2);
                if (onStatus) onStatus('Failed to load face detection neural models. Please check internet access.');
                return false;
            }
        }
    },

    // Start video camera stream
    async startCamera(videoElement) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 640 },
                    height: { ideal: 480 },
                    facingMode: 'user'
                },
                audio: false
            });
            videoElement.srcObject = stream;
            return new Promise((resolve) => {
                videoElement.onloadedmetadata = () => {
                    videoElement.play();
                    resolve(true);
                };
            });
        } catch (err) {
            console.error('Camera access error: ', err);
            throw err;
        }
    },

    // Stop video camera stream
    stopCamera(videoElement) {
        if (videoElement && videoElement.srcObject) {
            const stream = videoElement.srcObject;
            const tracks = stream.getTracks();
            tracks.forEach(track => track.stop());
            videoElement.srcObject = null;
        }
    },

    // Detect single face with descriptor from video element
    async detectFace(videoElement) {
        if (!this.modelsLoaded) return null;
        const options = new faceapi.TinyFaceDetectorOptions({ inputSize: 320, scoreThreshold: 0.5 });
        const detection = await faceapi.detectSingleFace(videoElement, options)
            .withFaceLandmarks()
            .withFaceDescriptor();
        return detection;
    },

    // Calculate Euclidean distance between two 128-float descriptors
    euclideanDistance(desc1, desc2) {
        if (!desc1 || !desc2 || desc1.length !== desc2.length) return 1.0;
        let sum = 0;
        for (let i = 0; i < desc1.length; i++) {
            const diff = desc1[i] - desc2[i];
            sum += diff * diff;
        }
        return Math.sqrt(sum);
    },

    // Find best match in list of employees
    // Threshold typically 0.52 to 0.58 for TinyFace + FaceRecognition
    findBestMatch(queryDescriptor, employees, threshold = 0.55) {
        let bestMatch = null;
        let minDistance = 999;

        for (const emp of employees) {
            if (!emp.faceDescriptor) continue;
            try {
                const empDesc = typeof emp.faceDescriptor === 'string'
                    ? JSON.parse(emp.faceDescriptor)
                    : emp.faceDescriptor;

                const dist = this.euclideanDistance(queryDescriptor, empDesc);
                if (dist < minDistance) {
                    minDistance = dist;
                    bestMatch = emp;
                }
            } catch (e) {
                console.error('Error parsing employee descriptor', e);
            }
        }

        if (bestMatch && minDistance <= threshold) {
            // Confidence formula: distance 0 => 100%, distance threshold => 70%
            const confidence = Math.min(99.9, Math.max(70.0, Math.round((1 - (minDistance / 1.1)) * 1000) / 10));
            return {
                matched: true,
                employee: bestMatch,
                distance: minDistance,
                confidence: confidence
            };
        }

        return { matched: false, distance: minDistance };
    },

    // Capture snapshot image from video as Base64 JPEG
    captureSnapshot(videoElement, maxWidth = 320) {
        try {
            const canvas = document.createElement('canvas');
            const ratio = videoElement.videoHeight / videoElement.videoWidth;
            canvas.width = maxWidth;
            canvas.height = Math.round(maxWidth * ratio);
            const ctx = canvas.getContext('2d');
            ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);
            return canvas.toDataURL('image/jpeg', 0.85);
        } catch (e) {
            console.error('Snapshot capture failed', e);
            return null;
        }
    }
};

window.FaceAttendanceEngine = FaceAttendanceEngine;
