using UnityEngine;
using System.Collections;

public class MouseManager : MonoBehaviour {
	
	GameObject target = null; //last thing clicked on by the player
	
	float mouseTimer = 0f; //amount of time mouse has been pressed
	
	Vector3 lastClickPos; //last position mouse was clicked at
	
	Vector3 currentMousePos3D; //holds the current mouse position

	public float swipeSpeed = 250f; //speed mutliplier added to a swipe
	float panSpeed = 0.05f; //camera pan speed, non-swipe
	
	public float swipethreshold = 20f; //time/distance ratio for mouse movement to be a swipe

	public float holdTime = 1.0f; //how the long the mouse has to be held down to be considered "held down"

	public GameObject newTool; //tool that is created when player holds the mouse down

	bool toolCreated = false; //flag, determines if a tool was just created
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		//check for a mouse releases
		OnMouseRelease();
		
		//check for mouse clicks
		OnMouseClick();
		
		//if the mouse is held down
		if(Input.GetMouseButton(0)){
			
			//update the mouse timer
			mouseTimer += Time.deltaTime;
			
			//update the current mouse position
			currentMousePos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			/*
			 * 
			 * Touch behavior works a bit differently depending if we're on mobile or
			 * desktop platforms. The following is mostly for camera panning and determining
			 * if the player is trying to create a new tool.
			 * 
			 */ 

			//if we're not on android
			if(Application.platform != RuntimePlatform.Android){


				/*
				 * Camera Panning for Desktop
				 * 
				 * Only pans when the player doesn't have a target.
				 */ 
				if(!target){
					//get mouse x movement
					float x = 1.0f * Input.GetAxis("Mouse X");
					float y = 1.0f * Input.GetAxis("Mouse Y");
					//translate the camera by that movement
					Camera.main.transform.Translate(-x, -y,0 );
				}

				/*
				 * Continuous Mouse Press (hold) Detection
				 * 
				 * If the mouse has been held for longer than the holdTime.
				 * If the mouse has not moved from where it was clicked.
				 */ 
				if(mouseTimer > holdTime && currentMousePos3D == lastClickPos){
					//Create a new tool, unless one was just made
					if(!toolCreated){
						//location to place the new tool, at the mouse click position
						Vector3 toolLoc = new Vector3(lastClickPos.x, lastClickPos.y, 0);
						//create the new tool
						Instantiate(newTool, toolLoc, Quaternion.identity).name = newTool.name;

						toolCreated = true; //set flag since a tool was just created
					}
				}
			}
			//if we're on android
			else{

				/*
				 * Camera Panning for Android
				 * 
				 */ 
				//if the player is moving a finger across the screen.
				if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) {

					//only pan the camera if there's no target
					if(!target){
						//get touch movement vector
						Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
						//translate camera by movement vector and pan speed
						Camera.main.transform.Translate(-touchDeltaPosition.x * panSpeed, 
						                                -touchDeltaPosition.y * panSpeed, 
						                                0);
					}
				}

				/*
				 * Continuous Touch (hold) Detection
				 * 
				 * If the touch lasts longer than the holdTime
				 * If the players finger is not moving.
				 */
				if(Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Moved && !target && mouseTimer > holdTime){

					//check if a tool was just created
					if(!toolCreated){
						//location to place the new tool. Adding 1f to the y so the players finger doesn't cover the tool.
						Vector3 toolLoc = new Vector3(lastClickPos.x, lastClickPos.y+1f, 0);

						//create the tool
						Instantiate(newTool, toolLoc, Quaternion.identity).name = newTool.name;

						//set flag since tool was just created
						toolCreated = true;
					}
				}
			}
        }
    }
    
	//physics update
    void FixedUpdate(){
		//if the player has some target
		if(target != null){
			//if the target is a player tool. Try to move the tool to the mouse position. 
			//Check the mouse timer to add some slack, maybe it was clicked by accident.
			if(target.tag == "Tool" && mouseTimer > 0.1f){
				
				//calculate the distance between where the target was clicked, and the current mouse position
				float distance = Vector3.Distance(lastClickPos, currentMousePos3D);
				
				//if the mouse has moved since clicking the object, player is trying to move it
				if(distance > 0){
					//move the target towards the target pos
					target.rigidbody2D.MovePosition(new Vector2(currentMousePos3D.x, currentMousePos3D.y));

				}
			}
		}
	}
	
	
	
	/* Checks for a left mouse click.
	 */
	void OnMouseClick(){
		//if left mouse button was clicked
		if(Input.GetMouseButtonDown(0)){

			//stop the camera, in case it was moving
			Camera.main.rigidbody2D.velocity = Vector2.zero;
			
			//reset the mouse timer, this is a new click
			mouseTimer = 0f;
			
			//get the mouse vector and convert it to 2d
			lastClickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 mousePos2D = new Vector2(lastClickPos.x, lastClickPos.y);

			//raycast direction vector. Zero since we're casting "into" the screen
			Vector2 dir = Vector2.zero;
			
			//ray cast in the mouse position and zero vector
			RaycastHit2D hit = Physics2D.Raycast(mousePos2D, dir);
			
			//did the raycast hit something with a collider?
			if(hit.collider != null){

				//if so, check if it's a selectable object such as a tool
				if(hit.collider.gameObject.tag == "Tool"){
					//set the target
					target = target = hit.transform.gameObject;
				}
			}
		}
	}
	
	/* Checks if the mouse was released.
	 * Resets the current target.
	 */
	void OnMouseRelease(){
		//if the mouse was released...
		if(Input.GetMouseButtonUp(0)){

			//Check if the player has a target
			if(target != null){

				//check if a player simply tapped a tool
				//if the mouse was released in less that 0.3f, then it was probably just a click
				if(target.tag == "Tool" && mouseTimer < 0.3f){
					//do whatever a tool should do when clicked on
					target.GetComponent<ChangeSpringType>().ChangeSpring();
				}

				//Release the target
				target = null;
			}
			//else, the player did not have a target
			else{

				//if the player did not have a target, and just released the mouse,
				//they may be trying to do a swipe gesture
				SwipeGesture ();
			}

            //restart the timer for the next mouse click
			mouseTimer = 0f;

			//if a tool was created, reset the flag
			if(toolCreated){
				toolCreated = false;
			}
		}
	}
	
	/*
	 * Calculates a distance / time ratio between the current mouse position
	 * and the last known mouse click position.
	 * 
	 * Return the ratio value.
	 */
	float MouseMoveX(){
		//get the distance the mouse traveled
		float distance = currentMousePos3D.x - lastClickPos.x;
		
		//divide it by how long the mouse was held to determine the movement ratio
		distance /= mouseTimer;
		
		return distance;
	}

	float MouseMoveY(){
		//get the distance the mouse traveled
		float distance = currentMousePos3D.y - lastClickPos.y;
		
		//divide it by how long the mouse was held to determine the movement ratio
		distance /= mouseTimer;
		
		return distance;
	}


	/*
	 * Determines if the user is performing a swipe gesture.
	 */ 
	void SwipeGesture(){
		//get mouse x direction movement ratio
		float xMov = MouseMoveX();
		float yMov = MouseMoveY();

		//create a movement force based on the x direction movement ratio
		Vector3 moveForce = new Vector3(-xMov, -yMov, 0);
		
		//if the x movement ratio exceeds the swipe threshold, user is performing a swipe
		if(Mathf.Abs(xMov) > swipethreshold || yMov > swipethreshold){
			//stop any camera movement
			Camera.main.rigidbody2D.velocity = Vector3.zero;
			//apply swipe force to the camera
			Camera.main.rigidbody2D.AddForce(moveForce*swipeSpeed);
		}
	}
}
