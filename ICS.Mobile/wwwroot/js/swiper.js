
function SwipeHandler(swipeElement, deleteButton, macroButton) {
    var swipeThreshold = 30;  // Buffer of px of movement before swipe is recognized
    var startX;
    var currentX;
    var isSwiping = false;
    var rightOpenTimer;
    var leftOpenTimer;

    swipeElement.addEventListener('touchstart', handleStart, false);
    swipeElement.addEventListener('mousedown', handleStart, false);

    function handleStart(e) {
        startX = e.touches ? e.touches[0].clientX : e.clientX;
        isSwiping = true;
        deleteButton.style.visibility = 'visible';
        macroButton.style.visibility = 'visible';
        swipeElement.style.transition = 'none';
        deleteButton.style.transition = 'none';
        macroButton.style.transition = 'none';

        var swipeElementHeight = swipeElement.offsetHeight;
        deleteButton.style.height = `${swipeElementHeight}px`;
        macroButton.style.height = `${swipeElementHeight}px`;
        //e.preventDefault();
    }

    document.addEventListener('touchmove', handleMove, false);
    document.addEventListener('mousemove', handleMove, false);

    function handleMove(e) {
        if (!isSwiping) return;
        currentX = (e.touches ? e.touches[0].clientX : e.clientX) - startX;

        if (Math.abs(currentX) > swipeThreshold) {
            var effectiveX = Math.abs(currentX) - swipeThreshold;
            var maxSwipeDistance = 75;
            var scalingFactor = 0.75;
            var adjustedEffectiveX = effectiveX * scalingFactor;
            var movement = generateFriction(adjustedEffectiveX, maxSwipeDistance) * (currentX > 0 ? 1 : -1);
            swipeElement.style.transform = `translateX(${movement}px)`;

            if (currentX > 0) {
                // Swipe right and reveal our buttons
                let buttonVisibility = Math.min(1, (movement - 30) / 40);  // reveal speed.
                deleteButton.style.opacity = buttonVisibility;
                deleteButton.style.transform = `translateX(${Math.max(-55, movement - 55)}px)`;
                macroButton.style.transform = 'translateX(50px)';
                macroButton.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
                macroButton.style.opacity = 0;
            } else {
                // Swipe left
                let buttonVisibility = Math.min(1, (-movement - 30) / 40);
                macroButton.style.opacity = buttonVisibility;
                macroButton.style.transform = `translateX(${Math.min(55, movement + 55)}px)`;
                deleteButton.style.transform = 'translateX(-50px)';
                deleteButton.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
                deleteButton.style.opacity = 0;
            }
        }
    }

    function generateFriction(x, max) {
        if (x > max) x = max;
        return max * (1 - Math.exp(-3 * x / max));
    }

    document.addEventListener('touchend', handleEnd, false);
    document.addEventListener('mouseup', handleEnd, false);

    function handleEnd() {
        isSwiping = false;
        swipeElement.style.transition = 'transform 1.0s ease'; // checklist item
        deleteButton.style.transition = 'transform 0.4s ease, opacity 0.7s ease'; // make little guys ease faster
        macroButton.style.transition = 'transform 0.4s ease, opacity 0.7s ease'; //  so it looks like its running away!

        clearTimeout(currentX > 0 ? rightOpenTimer : leftOpenTimer);

        if (Math.abs(currentX) >= 150) // exceed swipe threshold so it feels like more effort to make it "park"
        {
            var timer = setTimeout(function () {
                swipeElement.style.transform = '';
                if (currentX > 0) {
                    // Right swipe hide
                    deleteButton.style.transform = 'translateX(-50px)';
                    deleteButton.style.opacity = 0;
                } else {
                    // Left swipe hide
                    macroButton.style.transform = 'translateX(50px)';
                    macroButton.style.opacity = 0;
                }
            }, 2250);

            if (currentX > 0) {
                rightOpenTimer = timer;
            } else {
                leftOpenTimer = timer;
            }
        } else {
            // back in the barn you go
            swipeElement.style.transform = '';
            if (currentX > 0) {
                deleteButton.style.transform = 'translateX(-50px)';
                deleteButton.style.opacity = 0;
            } else {
                macroButton.style.transform = 'translateX(50px)';
                macroButton.style.opacity = 0;
            }
        }
    }
}








// STYLE STUFF NEEDED FOR CONTAINER AND BUTTONS
//.DispatchDetailCheckListContainer {
//    padding-bottom: 1rem;
//}

//.ChecklistItemDeleteButton {
//    position: absolute;
//    left: 0;
//    top: 0;
//    width: 50px;
//    height: 50px;
//    margin-left: -5px;
//    display: flex;
//    justify-content: center;
//    align-items: center;
//    opacity: 0;
//    transform: translateX(-75px);
//    transition: transform 4s ease, opacity 4s ease;

//}

//    .DeleteButton:active {
//    box - shadow: inset 0px 0px 10px rgba(0, 0, 0, 0.40);
//}

//.ChecklistSwipeContainer {
//    position: relative;
//    overflow: hidden;

//}

//.DispatchDetailChecklistItem {
//    position: relative;
//    transition: transform 0.6s ease;
//}




//<script defer>


//    function SwipeRight(element, deleteButton) {

//            var swipeThreshold = 16;  // Buffer of px of movement before swipe is recognized
//    var startX;
//    var currentX;
//    var isSwiping = false;
//    var openTimer;

//    element.addEventListener('touchstart', handleStart, false);
//    element.addEventListener('mousedown', handleStart, false);

//    function handleStart(e) {
//        startX = e.touches ? e.touches[0].clientX : e.clientX;
//    isSwiping = true;
//    element.style.transition = 'none';
//    deleteButton.style.transition = 'none';
//    deleteButton.style.visibility = 'visible';
//    var elementHeight = element.offsetHeight;
//    deleteButton.style.height = `${elementHeight}px`;
//    e.preventDefault();
//            }

//    document.addEventListener('touchmove', handleMove, false);
//    document.addEventListener('mousemove', handleMove, false);

//    function handleMove(e) {
//                if (!isSwiping) return;
//    currentX = (e.touches ? e.touches[0].clientX : e.clientX) - startX;

//                // Adjust currentX by subtracting the threshold once exceeded
//                if (Math.abs(currentX) > swipeThreshold && currentX > 0) {
//                    var effectiveX = currentX - swipeThreshold;
//    var maxSwipeDistance = 75;
//    var movement = generateFriction(effectiveX, maxSwipeDistance);
//    element.style.transform = `translateX(${movement}px)`;
//    let buttonVisibility = Math.min(1, (movement - 30) / 20);
//    deleteButton.style.opacity = buttonVisibility;
//    deleteButton.style.transform = `translateX(${Math.max(-55, movement - 55)}px)`;
//                }
//            }

//    function generateFriction(x, max) {
//                if (x > max) x = max;
//    return max * (1 - Math.exp(-2 * x / max));
//            }

//    document.addEventListener('touchend', handleEnd, false);
//    document.addEventListener('mouseup', handleEnd, false);

//    function handleEnd() {
//        isSwiping = false;
//    element.style.transition = 'transform 1.0s ease';  // Checklist item transition back
//    deleteButton.style.transition = 'transform 0.4s ease, opacity 0.7s ease'; // Little guy runs faster, fade slower
//                if (currentX >= 74) {
//        openTimer = setTimeout(function () {
//            element.style.transform = '';
//            deleteButton.style.transform = 'translateX(-50px)';
//            deleteButton.style.opacity = 0;
//        }, 2000);
//                } else {
//        element.style.transform = '';
//    deleteButton.style.transform = 'translateX(-50px)';
//    deleteButton.style.opacity = 0;
//                }
//            }
//        }

//</script>

function initializeSwipeEvents(dotNetReference) {

}
